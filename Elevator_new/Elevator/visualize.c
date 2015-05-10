#include<stdio.h>
#include<string.h>
#include<stdlib.h>
#include<ncurses.h>

#define true 1
#define false 0

int global_tick = 0;
int waitup[21], waitdown[21];
int currfloor[4], target[4], people[4], currheight[4], isidle[4];
char* debuglinebuf = NULL;
unsigned int simulate_speed = 0;

static void debug(char* linebuf) {
    if (debuglinebuf)
        free(debuglinebuf);
    debuglinebuf = strdup(linebuf);
}

static void colorprint(int line, int col, int value, int color) {
    attron(COLOR_PAIR(color));
    mvprintw(line, col, "%d", value);
    attroff(COLOR_PAIR(color));
}

static void repaint() {
    erase();

    int i;
    attron(COLOR_PAIR(4));
    mvprintw(2, 0, "waitUp");
    mvprintw(3, 0, "waitDown");
    mvprintw(5, 0, "Floor");
    mvprintw(0, 0, "Speed %dx, Tick %d", simulate_speed, global_tick);
    attroff(COLOR_PAIR(4));
    int basecol = 10;
    for (i=0; i<=20; i++) {
        colorprint(2, basecol + i*5, waitup[i], waitup[i] ? 5 : 4);
        colorprint(3, basecol + i*5, waitdown[i], waitdown[i] ? 5 : 4);
        colorprint(5, basecol + i*5, i, 4);
    }
    attron(COLOR_PAIR(4));
    mvprintw(5, basecol + 22*5, "Curr => Target", currfloor[i], target[i]);
    attroff(COLOR_PAIR(4));

    for (i=0; i<4; i++) {
        int line = 7 + 2*i;

        attron(COLOR_PAIR(4));
        mvprintw(line, 0, "Elev %d", i);
        attroff(COLOR_PAIR(4));

        attron(COLOR_PAIR(1));
        int j;
        if (target[i] < currfloor[i]) { // go down
            for (j = basecol + target[i]*5; j < basecol + currheight[i]/2; j++)
                mvprintw(line, j, "<");
        }
        else if (target[i] > currfloor[i]) {
            for (j = basecol + currheight[i]/2; j < basecol + target[i]*5; j++)
                mvprintw(line, j, ">");
        }
        
        if (target[i] != currfloor[i])
            mvprintw(line, basecol + target[i]*5, ".");
        attroff(COLOR_PAIR(1));

        attron(COLOR_PAIR(4));
        mvprintw(line, basecol + 22*5, "%5d => %5d", currfloor[i], target[i]);
        attroff(COLOR_PAIR(4));
        
        int color = (target[i] == currfloor[i] ? 3 : 2);
        attron(COLOR_PAIR(color));
        mvprintw(line, basecol + currheight[i]/2, "%2d", people[i]);
        attroff(COLOR_PAIR(color));
    }

    if (debuglinebuf) {
        attron(COLOR_PAIR(4));
        mvprintw(30, 0, "%s", debuglinebuf);
        attroff(COLOR_PAIR(4));
    }
    
    refresh();
}

static void parse_line(char* linebuf, int buflen) {
    linebuf[buflen] = '\0';

    int tick;
    if (1 == sscanf(linebuf, "#Tick %d", &tick)) {
        global_tick = tick;
        repaint();
        usleep(1000 * 1000 / simulate_speed);
        return;
    }
    int elev;
    if (1 == sscanf(linebuf, "[Elevator%d]", &elev)) {
        if (elev >= 4)
            return;

        if (strstr(linebuf, "It is Idle")) {
            isidle[elev] = 1;
            return;
        }
        if (strstr(linebuf, "CLOSE"))
            ;
        if (strstr(linebuf, "OPEN"))
            ;

        int c, t, h;
        if (4 == sscanf(linebuf, "[Elevator%d]: now at %d floor\t is running to %d floor\t Height %d",
            &elev, &c, &t, &h)) {

            currfloor[elev] = c;
            target[elev] = t;
            currheight[elev] = h;
        }
        return;
    }
    int passenger_no, floor, target;
    if (3 == sscanf(linebuf, "Passenger[Xiao_%d] is coming to floor %d target %d", &passenger_no, &floor, &target)
     || 3 == sscanf(linebuf, "Passenger[Sen_%d] is coming to floor %d target %d", &passenger_no, &floor, &target)
     || 3 == sscanf(linebuf, "Passenger[Ben_%d] is coming to floor %d target %d", &passenger_no, &floor, &target)
    ) {
        if (floor > 20 || target > 20)
            return;
        if (target > floor)
            waitup[floor]++;
        else
            waitdown[floor]++;
        return;
    }
    if (2 == sscanf(linebuf, "=>Xiao_%d leave Elevator%d", &passenger_no, &elev)
      ||2 == sscanf(linebuf, "=>Sen_%d leave Elevator%d", &passenger_no, &elev)
      ||2 == sscanf(linebuf, "=>Ben_%d leave Elevator%d", &passenger_no, &elev)
    ) {
        if (elev >= 4)
            return;
        people[elev]--;
        return;
    }
    char direction[10];
    if (3 == sscanf(linebuf, "=>Xiao_%d enter Elevator%d direction %s", &passenger_no, &elev, direction)
      ||3 == sscanf(linebuf, "=>Sen_%d enter Elevator%d direction %s", &passenger_no, &elev, direction)
      ||3 == sscanf(linebuf, "=>Ben_%d enter Elevator%d direction %s", &passenger_no, &elev, direction)
    ) {
        if (elev >= 4)
            return;
        people[elev]++;
        if (strcmp(direction, "Up") == 0)
            waitup[currfloor[elev]]--;
        else if (strcmp(direction, "Down") == 0)
            waitdown[currfloor[elev]]--;
        else
            debug(linebuf);
        return;
    }
}

static void nextline() {
#define MAX_LINE_LEN 1024
    static FILE *log = NULL;
    static char linebuf[MAX_LINE_LEN+1] = {0};
    static int buflen = 0;

    while (true) {
        unsigned char c = getchar();
        if (c == 255) // EOF
            return;
        if (c == '\n')
            break;
        if (c >= 128) // not displayable, ignore
            continue;
        linebuf[buflen++] = c;
        if (buflen == MAX_LINE_LEN) // as if there is a \n
            break;
    }

    // end of line
    parse_line(linebuf, buflen);
    buflen = 0;
}

int main(int argc, char** argv)
{
    if (argc == 2)
        simulate_speed = atoi(argv[1]);
    if (simulate_speed <= 0)
        simulate_speed = 5;

    initscr();
    curs_set(0);
    noecho();
    start_color();
    init_pair(1, COLOR_BLACK, COLOR_WHITE);
    init_pair(2, COLOR_BLACK, COLOR_GREEN);
    init_pair(3, COLOR_BLACK, COLOR_YELLOW);
    init_pair(4, COLOR_WHITE, COLOR_BLACK);
    init_pair(5, COLOR_WHITE, COLOR_RED);
    while (true) {
        nextline();
    }
    endwin();
}
