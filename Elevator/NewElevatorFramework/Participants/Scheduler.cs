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
            // if history is too short, don't change mode
            if (currHistoryLength * 2 > HISTORY_LENGTH) {
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
            }
        }
        // END elevator state judgement

        //-----------------Here add the schedule algorithm----------------
        //-----------------Here add the schedule algorithm----------------
        bool[] ElevValid;
        bool[] ElevHaveTrueTask;
        List<int>[] ElevHistoryReq;
        int[] HistoryReqLength;
        Direction[] ElevRequestDirection;

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
            ElevRequestDirection[elevator] = dir;
            HistoryReqLength[elevator]++;
        }

        void onOpenDoorSchedule(object sender, EventArgs e)
        {
            IElevator elev = sender as IElevator;
            if (HistoryReqLength[elev.ID] > 0 && elev.CurrentFloor == elev.CurrentTargetFloor && ElevHistoryReq[elev.ID][HistoryReqLength[elev.ID] - 1] == elev.CurrentFloor)
            {
                popHistoryReq(elev.ID);
                if (elev.HistoryDirection == Direction.Up && elev.FreeCapability != elev.Capability)
                {
                    directionButtonPanels[elev.CurrentTargetFloor].buttonReleased(1);
                }
                else if (elev.HistoryDirection == Direction.Down && elev.FreeCapability != elev.Capability)
                {
                    directionButtonPanels[elev.CurrentTargetFloor].buttonReleased(0);
                }
                else if (elev.FreeCapability == elev.Capability)
                {
                    bool pendingUp = (directionButtonPanels[elev.CurrentTargetFloor].Buttons[Up].State == ButtonState.Pressed);
                    bool pendingDown = (directionButtonPanels[elev.CurrentTargetFloor].Buttons[Down].State == ButtonState.Pressed);
                    Direction toRelease = Direction.No;
                    if (pendingUp && pendingDown)
                    {
                        toRelease = elev.HistoryDirection;
                    }
                    else if (pendingUp)
                    {
                        toRelease = Direction.Up;
                    }
                    else if (pendingDown)
                    {
                        toRelease = Direction.Down;
                    }

                    if (toRelease >= 0)
                    {
                        directionButtonPanels[elev.CurrentTargetFloor].buttonReleased(toRelease == Direction.Up ? 1 : 0);
                        elev.setCurrentDirection(toRelease);
                    }
                }
            }
            else if (elev.FreeCapability == elev.Capability)
            {
                bool pendingUp = (directionButtonPanels[elev.CurrentTargetFloor].Buttons[Up].State == ButtonState.Pressed);
                bool pendingDown = (directionButtonPanels[elev.CurrentTargetFloor].Buttons[Down].State == ButtonState.Pressed);
                Direction toRelease = Direction.Up;
                if (pendingUp)
                {
                    toRelease = Direction.Up;
                }
                else if (pendingDown)
                {
                    toRelease = Direction.Down;
                }

                if (toRelease >= 0)
                {
                    directionButtonPanels[elev.CurrentTargetFloor].buttonReleased(toRelease == Direction.Up ? 1 : 0);
                    elev.setCurrentDirection(toRelease);
                }
            }
        }

        void onCloseDoorSchedule(object sender, EventArgs e)
        {
            IElevator elev = sender as IElevator;
            if (elev.FreeCapability != elev.Capability) // 如果电梯里有人
            {
                List<int> ls = elev.ButtonPanel.getAllPressedButtonsIndex();
                if (ls.Count != 0)
                {
                    // 如果电梯上行，则取栈顶和电梯内最低目标的较低者
                    if (elev.HistoryDirection == Direction.Up)
                    {
                        if (HistoryReqLength[elev.ID] == 0 || ls[0] < ElevHistoryReq[elev.ID][HistoryReqLength[elev.ID] - 1])
                        {
                            elev.setTargetFloor(ls[0]);
                        }
                        else
                        {
                            elev.setTargetFloor(ElevHistoryReq[elev.ID][HistoryReqLength[elev.ID] - 1]);
                        }
                    }
                    // 如果电梯下行，则取栈顶和电梯内最高目标的较高者
                    else if (elev.HistoryDirection == Direction.Down)
                    {
                        if (HistoryReqLength[elev.ID] == 0 || ls[ls.Count - 1] > ElevHistoryReq[elev.ID][HistoryReqLength[elev.ID] - 1])
                        {
                            elev.setTargetFloor(ls[ls.Count - 1]);
                        }
                        else
                        {
                            elev.setTargetFloor(ElevHistoryReq[elev.ID][HistoryReqLength[elev.ID] - 1]);
                        }
                    }
                    //如果电梯没有方向
                    else
                    {
                        elev.setTargetFloor(ls[0]);
                    }
                }
                else
                {
                    Utility.logError("Someone in elevator but not pressed any button");
                }
            }
            else // 如果电梯空闲
            {
                if (ElevMode == ElevModeRandom)
                {
                }
                else if (ElevMode == ElevModeComeIn)
                {
                    elev.setTargetFloor(0);
                }
                else if (ElevMode == ElevModeGoOut)
                {
                    elev.setTargetFloor(elev.HighestFloorCanArrive);
                }
            }
             //处理电梯满时改变目标的情况，把一些要响应的楼层加回未响应请求
            if (HistoryReqLength[elev.ID]!=0 && elev.FreeCapability < 60 && elev.CurrentTargetFloor == ElevHistoryReq[elev.ID][HistoryReqLength[elev.ID] - 1])
            {
                List<int> ls = elev.ButtonPanel.getAllPressedButtonsIndex();
                if (ls.Count > 0)
                {
                    if (elev.HistoryDirection == Direction.Up)
                    {
                        elev.setTargetFloor(ls[0]);
                        for (int j = HistoryReqLength[elev.ID]; j > 0; j--)
                        {
                            if (ElevHistoryReq[elev.ID][j - 1] < ls[0])
                            {
                                directionButtonPanels[ElevHistoryReq[elev.ID][j - 1]].addNewlyPressedButtonNotification(Direction.Up);
                                popHistoryReq(elev.ID);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        elev.setTargetFloor(ls[ls.Count - 1]);
                        for (int j = HistoryReqLength[elev.ID]; j > 0; j--)
                        {
                            if (ElevHistoryReq[elev.ID][j - 1] > ls[ls.Count - 1])
                            {
                                directionButtonPanels[ElevHistoryReq[elev.ID][j - 1]].addNewlyPressedButtonNotification(Direction.Down);
                                popHistoryReq(elev.ID);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void despatchQueriesToElev()     //our algorithm
        {
            for (int i = 0; i < ElevValid.Length; i++)          //记录哪几个电梯可用和是否有真正的响应目标
            {
                if (elevators[i].IsDoorOpening || elevators[i].FreeCapability < 60)
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
           
            for (int i = 0; i < elevators.Length; i++)      //分配响应
            {
                // 可用的电梯：不满且不在开门状态
                if (ElevValid[i])
                {
                    if (elevators[i].HistoryDirection == Direction.Up && ElevHaveTrueTask[i])
                    {
                        // 必须向上走
                        if (ElevRequestDirection[i] == Direction.Down) // 已有请求方向与当前请求方向不符
                            continue;
                        for (int j = elevators[i].CurrentFloor + 1; j <= elevators[i].CurrentTargetFloor; j++)
                        {
                            if (directionButtonPanels[j].checkNewlyPressedButtonNotification(Direction.Up) && elevators[i].setTargetFloor(j))
                            {
                                pushHistoryReq(i, j, Direction.Up);
                                elevators[i].setTargetFloor(j);
                                directionButtonPanels[j].removeNewlyPressedButtonNotification(Direction.Up);
                                break;
                            }
                        }
                    }
                    
                    else if (elevators[i].HistoryDirection == Direction.Down &&
                        (ElevHaveTrueTask[i] || ElevMode == ElevModeComeIn && elevators[i].CurrentFloor >= 2))
                    {
                        // 必须向下走
                        if (ElevRequestDirection[i] == Direction.Up) // 已有请求方向与当前请求方向不符
                            continue;
                        for (int j = elevators[i].CurrentFloor - 2; j >= elevators[i].CurrentTargetFloor && j >= 0; j--)
                        {
                            if (directionButtonPanels[j].checkNewlyPressedButtonNotification(Direction.Down) && elevators[i].setTargetFloor(j))
                            {
                                pushHistoryReq(i, j, Direction.Down);
                                elevators[i].setTargetFloor(j);
                                directionButtonPanels[j].removeNewlyPressedButtonNotification(Direction.Down);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // 向上向下都可以
                        int minDiff = maxFloorCounts;
                        int minDiffFloor = -1;
                        for (int j = 0; j < maxFloorCounts; j++)
                        {
                            if (directionButtonPanels[j].HasNewlyPressedButton && elevators[i].setTargetFloor(j))
                            {
                                int diff = j - elevators[i].CurrentFloor;
                                if (diff < 0)
                                    diff = -diff;
                                if (diff < minDiff)
                                {
                                    minDiff = diff;
                                    minDiffFloor = j;
                                }
                            }
                        }
                        if (minDiffFloor != -1)
                        {
                            elevators[i].setTargetFloor(minDiffFloor);

                            bool pendingUp = directionButtonPanels[minDiffFloor].checkNewlyPressedButtonNotification(Direction.Up);
                            bool pendingDown = directionButtonPanels[minDiffFloor].checkNewlyPressedButtonNotification(Direction.Down);
                            Direction dir;
                            if (pendingUp && pendingDown)
                            {
                                dir = elevators[i].CurrentDirection;
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
                            pushHistoryReq(i, minDiffFloor, dir);
                            directionButtonPanels[minDiffFloor].removeNewlyPressedButtonNotification(dir);
                            
                        }
                    }
                }
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
