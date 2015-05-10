/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines the schuduler,and the despatch algorithm should be 
 *      added here (It means that you can just modify this file and left others 
 *      unchanged to change the elevators' despatch algorithm)
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewElevatorFramework
{
    class Scheduler
    {
        ButtonPanelOutsideElev[] directionButtonPanels;
        DoorState[][] defenceDoorState;
        IElevator[] elevators;
        int maxFloorCounts;

        //properties
        public ButtonPanelOutsideElev[] DirectionButtons { get { return directionButtonPanels; } }
        public DoorState[][] DefenceDoorState { get { return defenceDoorState; } }
        public int MaxFloorCounts { get { return maxFloorCounts; } }

        //constructor
        public Scheduler(int floorSum) { 
            maxFloorCounts = floorSum;
            directionButtonPanels = new ButtonPanelOutsideElev[maxFloorCounts];
            defenceDoorState = new DoorState[maxFloorCounts][];
            for (int i = 0; i < maxFloorCounts; i++) {
                directionButtonPanels[i] = new ButtonPanelOutsideElev(i);
                //the second dimention of defenceDoorState represents how many defence elevator doors at one floor
                // so it should be initialized at the action of binding with elevators
            }
        }

        //member methods
        public bool bindWithElevators(IElevator[] newElevators)
        {
            if (newElevators == null) {
                Utility.logError("Wrong argument");
                throw new ArgumentNullException();
            }
            elevators = newElevators;
            foreach (var elev in newElevators) {
                elev.addEventListener(EventType.DoorOpen, onElevatorDoorOpen);
                elev.addEventListener(EventType.DoorClose, onElevatorDoorClose);
            }
            //initialize the sizes of defence doors' number of each floor
            for (int i = 0; i < maxFloorCounts; i++) {
                defenceDoorState[i] = new DoorState[elevators.Length]; 
            }

            initElevState();
            initScheduler();
            return true;
        }

        // BEGIN elevator mode judgement
        const int ElevModeRandom = 0;
        const int ElevModeComeIn = 1;
        const int ElevModeGoOut = 2;
        int ElevMode = ElevModeRandom;

        const int Down = 0;
        const int Up = 1;

        // floors 0 and 1 are considered first floors
        const int FIRST_FLOOR = 1;
        const int HISTORY_LENGTH = 20;
        int currHistoryLength = 0;
        int[] weightBeforeOpenDoor;
        // last time door is opened
        int[] weightDiffHistory;

        // should be called when history is updated
        void updateElevMode()
        {
            int totalWeightIncrease = 0; // sum of weight diff history
            int totalWeightDiff = 0;     // sum of abs(weight diff history)
            for (int i = 0; i < currHistoryLength; i++)
            {
                totalWeightIncrease += weightDiffHistory[i];
                totalWeightDiff += (weightDiffHistory[i] > 0 ? weightDiffHistory[i] : -weightDiffHistory[i]);
            }

            int origElevMode = ElevMode;
            const double threshold = 0.5;
            if (totalWeightIncrease > totalWeightDiff * threshold)
                ElevMode = ElevModeComeIn;
            else if (totalWeightIncrease < -totalWeightDiff * threshold)
                ElevMode = ElevModeGoOut;
            else
                ElevMode = ElevModeRandom;
            if (origElevMode != ElevMode)
            {
                Utility.log("Elevator Mode changed to " +
                    (ElevMode == ElevModeComeIn ? "ElevModeComeIn" :
                    (ElevMode == ElevModeGoOut ? "ElevModeGoOut" :
                    "ElevModeRandom")));
            }
        }
        void onFirstFloorsDoorOpen(object sender, EventArgs e)
        {
            if (sender.GetType() is NewElevatorFramework.IElevator){
                Utility.logWarning("This event is not caused by elevators : Scheduler::abstractDoorEventHandler");
                return; 
            }
            IElevator elev = sender as IElevator;
            if (elev.CurrentFloor <= FIRST_FLOOR) {
                weightBeforeOpenDoor[elev.ID] = elev.Capability - elev.FreeCapability;
            }
        }
        void onFirstFloorDoorClose(object sender, EventArgs e)
        {            
            if (sender.GetType() is NewElevatorFramework.IElevator){
                Utility.logWarning("This event is not caused by elevators : Scheduler::abstractDoorEventHandler");
                return; 
            }
            IElevator elev = sender as IElevator;
            if (elev.CurrentFloor <= FIRST_FLOOR)
            {
                int weightAfterCloseDoor = elev.Capability - elev.FreeCapability;
                int weightDiff = weightAfterCloseDoor - weightBeforeOpenDoor[elev.ID];
                if (currHistoryLength == HISTORY_LENGTH)
                {
                    for (int i = 0; i < currHistoryLength - 1; i++)
                    {
                        weightDiffHistory[i] = weightDiffHistory[i + 1];
                    }
                    weightDiffHistory[currHistoryLength - 1] = weightDiff;
                }
                else
                {
                    weightDiffHistory[currHistoryLength++] = weightDiff;
                }
            }

            updateElevMode();
        }
        // should be called before simulation
        void initElevState()
        {
            weightDiffHistory = new int[HISTORY_LENGTH];
            weightBeforeOpenDoor = new int[elevators.Length];

            foreach (var elev in elevators)
            {
                elev.addEventListener(EventType.DoorOpen, onFirstFloorsDoorOpen);
                elev.addEventListener(EventType.DoorOpen, onOpenDoorSchedule);
                elev.addEventListener(EventType.DoorClose, onFirstFloorDoorClose);
                elev.addEventListener(EventType.DoorClose, onCloseDoorSchedule);
                elev.setCurrentDirection(Direction.No);
            }
        }
        // END elevator state judgement

        //-----------------Here add the schedule algorithm----------------
        bool[] ElevValid;
        bool[] ElevHaveTrueTask;
        List<int>[] ElevHistoryReq;
        int[] HistoryReqLength;
        Direction[] ElevRequestDirection;
        const int minFreeCapacity = 80;

        // round robin to floor 0 and 1
        int lastFirstFloor = 0;

        void initScheduler()
        {
            ElevValid = new bool[elevators.Length];
            ElevHaveTrueTask = new bool[elevators.Length];
            ElevHistoryReq = new List<int>[elevators.Length];
            for (int i = 0; i < elevators.Length; i++)
            {
                ElevHistoryReq[i] = new List<int>();
            }
            HistoryReqLength = new int[elevators.Length];
            ElevRequestDirection = new Direction[elevators.Length];
            for (int i = 0; i < elevators.Length; i++)
            {
                ElevRequestDirection[i] = Direction.No;
            }
        }

        int popHistoryReq(int elevator)
        {
            if (HistoryReqLength[elevator] == 0)
                return -1;
            int historyTarget = ElevHistoryReq[elevator][HistoryReqLength[elevator] - 1];
            HistoryReqLength[elevator]--;
            if (HistoryReqLength[elevator] == 0)
                ElevRequestDirection[elevator] = Direction.No;
            return historyTarget;
        }

        void pushHistoryReq(int elevator, int floor, Direction dir)
        {
            if (HistoryReqLength[elevator] < ElevHistoryReq[elevator].Count)
            {
                ElevHistoryReq[elevator][HistoryReqLength[elevator]] = floor;
            }
            ElevHistoryReq[elevator].Add(floor);
            if (ElevRequestDirection[elevator] != Direction.No && ElevRequestDirection[elevator] != dir)
            {
                Utility.logError("elevator request direction should never be changed");
            }
            ElevRequestDirection[elevator] = dir;
            HistoryReqLength[elevator]++;
        }

        int getTopHistoryReq(int elevator)
        {
            if (HistoryReqLength[elevator] == 0)
                return -1;
            return ElevHistoryReq[elevator][HistoryReqLength[elevator] - 1];
        }

        void changeElevatorDirection(IElevator elev, Direction dir)
        {
            if (dir != Direction.No)
            {
                directionButtonPanels[elev.CurrentFloor].buttonReleased(dir == Direction.Up ? 1 : 0);
            }
            elev.setCurrentDirection(dir);
        }

        void onOpenDoorSchedule(object sender, EventArgs e)
        {
            IElevator elev = sender as IElevator;
            if (elev.CurrentFloor != elev.CurrentTargetFloor)
            {
                Utility.logError("CurrentFloor " + elev.CurrentFloor + " != CurrentTargetFloor " + elev.CurrentTargetFloor);
            }

            // 根据之前保存的外部请求方向决定关门后的继续行进方向
            // 在开门的时候，只是设置当前方向，以让合适方向的乘客进来，而不需要管目标楼层。关门的时候再管目标楼层。
            List<int> ls = elev.ButtonPanel.getAllPressedButtonsIndex();
            if (ls.Count == 0)
            {
                if (HistoryReqLength[elev.ID] > 0)
                {
                    changeElevatorDirection(elev, ElevRequestDirection[elev.ID]);
                }
                else
                {
                    if (elev.CurrentFloor == 0)
                        changeElevatorDirection(elev, Direction.Up);
                    else if (elev.CurrentFloor == elev.HighestFloorCanArrive)
                        changeElevatorDirection(elev, Direction.Down);
                    else
                        changeElevatorDirection(elev, Direction.No);
                }
            }
            else
            {
                changeElevatorDirection(elev, elev.HistoryDirection);
            }

            // 如果到了目标楼层，从响应栈中清空
            if (getTopHistoryReq(elev.ID) == elev.CurrentFloor)
            {
                popHistoryReq(elev.ID);
            }
        }

        void onCloseDoorSchedule(object sender, EventArgs e)
        {
            IElevator elev = sender as IElevator;
            if (elev.IsEmpty)
            {
                if (HistoryReqLength[elev.ID] > 0)
                {
                    elev.setTargetFloor(getTopHistoryReq(elev.ID));
                }
                else
                {
                    elev.setCurrentDirection(Direction.No);
                }
                return;
            }

            List<int> ls = elev.ButtonPanel.getAllPressedButtonsIndex();
            if (ls.Count == 0)
            {
                Utility.logError("Someone in elevator but not pressed any button");
            }
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i] < elev.CurrentFloor && elev.CurrentDirection == Direction.Up ||
                    ls[i] > elev.CurrentFloor && elev.CurrentDirection == Direction.Down)
                {
                    Utility.logError("passenger enter wrong direction");
                }
                if (ls[i] == elev.CurrentFloor)
                {
                    Utility.logError("cannot goto current floor");
                }
            }

            if (elev.FreeCapability < minFreeCapacity)
                onCloseDoorOverWeight(elev, ls);
            else
                onCloseDoorDefaultSchedule(elev, ls);
        }

        void onCloseDoorDefaultSchedule(IElevator elev, List<int> ls)
        {
            // 如果电梯上行，则设置目标为栈顶和电梯内最低目标的较低者
            if (elev.HistoryDirection == Direction.Up)
            {
                if (HistoryReqLength[elev.ID] == 0 || ls[0] < getTopHistoryReq(elev.ID))
                {
                    if (ls[0] <= elev.CurrentFloor)
                    {
                        Utility.logError("Elevator " + elev.ID + " at floor " + elev.CurrentFloor + " and going up, but passenger pressed " + ls[0]);
                    }
                    elev.setTargetFloor(ls[0]);
                }
                else
                {
                    elev.setTargetFloor(getTopHistoryReq(elev.ID));
                }
            }
            // 如果电梯下行，则设置目标为栈顶和电梯内最高目标的较高者
            else if (elev.HistoryDirection == Direction.Down)
            {
                if (HistoryReqLength[elev.ID] == 0 || ls[ls.Count - 1] > getTopHistoryReq(elev.ID))
                {
                    if (ls[ls.Count - 1] >= elev.CurrentFloor)
                    {
                        Utility.logError("Elevator " + elev.ID + " at floor " + elev.CurrentFloor + " and going down, but passenger pressed " + ls[ls.Count - 1]);
                    }
                    elev.setTargetFloor(ls[ls.Count - 1]);
                }
                else
                {
                    elev.setTargetFloor(getTopHistoryReq(elev.ID));
                }
            }
            // 电梯没有方向，人就不可能进去
            else
            {
                Utility.logError("There are " + ls.Count + " people in elevator " + elev.ID + " without direction");
            }
        }

        // 如果电梯快满了，则直奔离当前楼层最近的电梯内请求楼层
        // 如果在当前楼层和最近的电梯内请求楼层之间有答应要处理的外部请求，则把这些请求扔回去，重新分配
        void onCloseDoorOverWeight(IElevator elev, List<int> ls)
        {
            if (elev.HistoryDirection == Direction.Up)
            {
                elev.setTargetFloor(ls[0]);
                // HistoryReq 栈里都是同方向的“搭车”请求，是有序的
                while (HistoryReqLength[elev.ID] > 0 && getTopHistoryReq(elev.ID) < ls[0])
                {
                    directionButtonPanels[popHistoryReq(elev.ID)].addNewlyPressedButtonNotification(Direction.Up);
                }
            }
            else if (elev.HistoryDirection == Direction.Down)
            {
                elev.setTargetFloor(ls[ls.Count - 1]);
                while (HistoryReqLength[elev.ID] > 0 && getTopHistoryReq(elev.ID) > ls[ls.Count - 1])
                {
                    directionButtonPanels[popHistoryReq(elev.ID)].addNewlyPressedButtonNotification(Direction.Down);
                }
            }
            else
            {
                Utility.logError("There are " + ls.Count + " people in elevator " + elev.ID + " without direction");
            }
        }

        void respondNewQueryWhileDoorOpening()
        {
            for (int i = 0; i < elevators.Length; i++)
            {
                // 开门期间如果没有方向，响应新来的请求并设置方向
                // 由于乘客是看历史方向的，方向只要不是 No 就不能改变，否则可能有两个方向的乘客进来。
                if (elevators[i].IsDoorOpening && elevators[i].CurrentDirection == Direction.No)
                {
                    bool pendingUp = directionButtonPanels[elevators[i].CurrentFloor].Buttons[Up].State == ButtonState.Pressed;
                    bool pendingDown = directionButtonPanels[elevators[i].CurrentFloor].Buttons[Down].State == ButtonState.Pressed;
                    if (pendingUp)
                        elevators[i].setCurrentDirection(Direction.Up);
                    else if (pendingDown)
                        elevators[i].setCurrentDirection(Direction.Down);
                }
            }
        }

        // return -1 for no answer
        // otherwise return floor number
        public int bestFloorToResponse(int i, out Direction dir)
        {
            dir = Direction.No;
            // 可用的电梯：不满且不在开门状态
            if (ElevValid[i])
            {
                if (ElevHaveTrueTask[i])
                {
                    if (elevators[i].HistoryDirection == Direction.Up)
                    {
                        // 必须向上走
                        if (ElevRequestDirection[i] == Direction.Down) // 已有请求方向与当前请求方向不符
                            return -1;
                        for (int j = elevators[i].CurrentFloor + 1; j <= elevators[i].CurrentTargetFloor; j++)
                        {
                            if (directionButtonPanels[j].checkNewlyPressedButtonNotification(Direction.Up) && elevators[i].isTargetValid(j))
                            {
                                dir = Direction.Up;
                                return j;
                            }
                        }
                        return -1;
                    }
                    else if (elevators[i].HistoryDirection == Direction.Down)
                    {
                        // 必须向下走
                        if (ElevRequestDirection[i] == Direction.Up) // 已有请求方向与当前请求方向不符
                            return -1;
                        for (int j = elevators[i].CurrentFloor - 1; j >= elevators[i].CurrentTargetFloor && j >= 0; j--)
                        {
                            if (directionButtonPanels[j].checkNewlyPressedButtonNotification(Direction.Down) && elevators[i].isTargetValid(j))
                            {
                                dir = Direction.Down;
                                return j;
                            }
                        }
                        return -1;
                    }
                    else
                    {
                        Utility.logError("should have direction when there are real tasks");
                        return -1;
                    }
                }
                else // no true task
                {
                    // 向上向下都可以
                    if (elevators[i].HistoryDirection != Direction.No)
                    {
                        Utility.logError("idle elevator should not have direction");
                    }
                    bool minDiffTheBetter = true;
                    if (directionButtonPanels[0].Buttons[Up].State == ButtonState.Pressed
                      || directionButtonPanels[1].Buttons[Up].State == ButtonState.Pressed)
                    {
                        if (elevators[i].CurrentFloor >= 2)
                        {
                            dir = Direction.Down;
                            lastFirstFloor = 1 - lastFirstFloor;
                            return lastFirstFloor;
                        }
                        minDiffTheBetter = true;
                    }
                    if (ElevMode == ElevModeGoOut) {
                        minDiffTheBetter = false;
                    }
                    int maxDiff = minDiffTheBetter ? maxFloorCounts : 0;
                    int bestFloor = -1;
                    for (int j = 0; j < maxFloorCounts; j++)
                    {
                        if (directionButtonPanels[j].HasNewlyPressedButton && elevators[i].isTargetValid(j))
                        {
                            int diff = j - elevators[i].CurrentFloor;
                            if (diff < 0)
                                diff = -diff;
                            if (minDiffTheBetter ? (maxDiff > diff) : (diff > maxDiff))
                            {
                                maxDiff = diff;
                                bestFloor = j;
                            }
                        }
                    }
                    if (bestFloor != -1)
                    {
                        bool pendingUp = directionButtonPanels[bestFloor].checkNewlyPressedButtonNotification(Direction.Up);
                        bool pendingDown = directionButtonPanels[bestFloor].checkNewlyPressedButtonNotification(Direction.Down);
                        if (pendingUp && pendingDown)
                        {
                            dir = elevators[i].CurrentDirection;
                            if (dir == Direction.No)
                            {
                                if (bestFloor < elevators[i].CurrentFloor)
                                    dir = Direction.Down;
                                else
                                    dir = Direction.Up;
                            }
                        }
                        else if (pendingDown)
                        {
                            dir = Direction.Down;
                        }
                        else if (pendingUp)
                        {
                            dir = Direction.Up;
                        }
                        else
                        {
                            Utility.logError("No pending ups nor down");
                            dir = Direction.No;
                        }
                        return bestFloor;
                    }
                    return -1;
                }
            }
            else { // 电梯不可用
                return -1;
            }
        }

        // called every tick
        public void despatchQueriesToElev()
        {
            respondNewQueryWhileDoorOpening();

            for (int i = 0; i < ElevValid.Length; i++)          //记录哪几个电梯可用和是否有真正的响应目标
            {
                if (elevators[i].IsDoorOpening || elevators[i].FreeCapability < minFreeCapacity)
                {
                    ElevValid[i] = false;
                }
                else
                {
                    ElevValid[i] = true;
                }
                List<int> ls = elevators[i].ButtonPanel.getAllPressedButtonsIndex();
                if (HistoryReqLength[i] == 0 && ls.Count == 0)
                {
                    ElevHaveTrueTask[i] = false;
                }
                else
                {
                    ElevHaveTrueTask[i] = true;
                }
            }

            bool changed = true;
            while (changed)
            {
                changed = false;

                int minScore = -1;
                int minElevator = -1;

                int[] target = new int[elevators.Length];
                Direction[] dir = new Direction[elevators.Length];
                for (int i = 0; i < elevators.Length; i++)      //分配响应
                {
                    target[i] = bestFloorToResponse(i, out dir[i]);

                    if (target[i] == -1)
                        continue;
                    if (dir[i] == Direction.No)
                    {
                        Utility.logError("direction should not be No");
                    }

                    const int averageWeight = 80;
                    int peopleInElevator = (elevators[i].Capability - elevators[i].FreeCapability) / averageWeight;
                    int distance = (elevators[i].CurrentFloor - target[i]);
                    if (distance < 0)
                        distance = -distance;
                    int score = peopleInElevator + distance;
                    if (minElevator == -1 || minScore > score)
                    {
                        minScore = score;
                        minElevator = i;
                    }
                }

                if (minElevator != -1)
                {
                    int selected = target[minElevator];
                    ElevValid[minElevator] = false;
                    pushHistoryReq(minElevator, selected, dir[minElevator]);
                    elevators[minElevator].setTargetFloor(selected);
                    directionButtonPanels[selected].removeNewlyPressedButtonNotification(dir[minElevator]);
                    changed = true;
                }
            }

            //解决请求丢失问题
            bool allIdle = true;
            for (int i = 0; i < elevators.Length; i++)
            {
                if (!elevators[i].IsIdle)
                {
                    allIdle = false;
                    break;
                }
            }
            bool isBug = false;
            if (allIdle)
            {
                for (int j = 0; j < maxFloorCounts; j++)
                {
                    if (directionButtonPanels[j].ButtonDisplay[0] == ButtonState.Pressed)
                    {
                        //directionButtonPanels[j].addNewlyPressedButtonNotification(Direction.Down);
                        isBug = true;
                    }
                    if (directionButtonPanels[j].ButtonDisplay[1] == ButtonState.Pressed)
                    {
                        //directionButtonPanels[j].addNewlyPressedButtonNotification(Direction.Up);
                        isBug = true;
                    }
                }
            }
            if (isBug)
            {
                Utility.logError("all elevators are Idle when there are requests");
            }
        }

        //----------------algorithm over----------------------------------

        public List<IButtonPanel> getNewlyPressedButtonPanelsOutside() {
            if (directionButtonPanels == null){
                Utility.logError("No panels outside : Scheduler::getNewlyPressedButtonPanelsOutside");
                throw new InvalidOperationException();
            }

            List<IButtonPanel> list = new List<IButtonPanel>();
            foreach (var panel in directionButtonPanels) {
                if (panel.HasNewlyPressedButton) {
                    list.Add(panel); 
                }  
            }
            return list;
        }

        // check whether the floor is under the control of this scheduler
        public bool checkFloor(int floorNumber) {
            if (floorNumber > maxFloorCounts || floorNumber < 0) {
                return false; 
            }
            return true;
        }

        //event handler
        private void onElevatorDoorOpen(object sender, EventArgs e) {
            abstractDoorEventHandler(sender, e, DoorState.Opened);
        }
        private void onElevatorDoorClose(object sender, EventArgs e) {
            abstractDoorEventHandler(sender, e, DoorState.Closed);            
        }
        private void abstractDoorEventHandler(object sender,EventArgs e,DoorState state) {
            if (sender.GetType() is NewElevatorFramework.IElevator){
                Utility.logWarning("This event is not caused by elevators : Scheduler::abstractDoorEventHandler");
                return; 
            }
            IElevator elev = sender as IElevator;
            int id = elev.ID;
            int stopFloor = elev.CurrentFloor;
            defenceDoorState[stopFloor][id] = state;
        }
    }
}
