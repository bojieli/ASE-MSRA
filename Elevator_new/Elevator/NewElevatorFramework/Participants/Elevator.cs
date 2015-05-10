/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines a specific implementation of the interface IElevator
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewElevatorFramework;

namespace NewElevatorFramework
{
    class Elevator : IElevator
    {
        //private member variables
        readonly int id;//this is a value will become the elevator's id
        IButtonPanel buttonPanel;
        readonly int capability; //it say the load bearing of weight
        int freeCapability;
        readonly int highestFloorCanArrive;
        readonly int floorHeight;
        Direction currentDirection;
        Direction historyDirection;
        int currentHeight;
        int currentTargetFloor;
        int doorOpeningTime;
        bool isDoorOpening;
        bool isIdle;
        const int NoFloor = -1;//this value represents -- not any floor,used for the currentTargetFloor
        const int DoorOpeningDuration = 5;
        event EventHandler doorOpenEvent;
        event EventHandler doorCloseEvent;
        delegate void OperationOfEventHandler(ref EventHandler handlerVar,ref EventHandler handler);

        //properties
        public int ID { get { return id; } }
        public IButtonPanel ButtonPanel { get { return buttonPanel; } }
        public int Capability { get { return capability; } }
        public int FreeCapability { get { return freeCapability; } }
        public int HighestFloorCanArrive { get { return highestFloorCanArrive; } }
        public Direction CurrentDirection { get { return currentDirection; } }
        public Direction HistoryDirection { get { return historyDirection; } }
        public int CurrentHeight { get { return currentHeight; } }
        public int CurrentTargetFloor { get { return currentTargetFloor; } }
        public int CurrentFloor
        {
            get
            {
                return (currentHeight / floorHeight);
            }
        }
        public bool IsDoorOpening { get { return isDoorOpening; } }
        public bool IsIdle { get { return isIdle; } }
            //properties which are not inherited from interface
        public int Velocity { get { return (int)MotionOfElevator.Velocity; } }
        public int DecelerationSpace { get { return (int)MotionOfElevator.DecelerationSpace; } }
        public bool IsEmpty { get { return freeCapability == capability; } }

        //constractor
        public Elevator(
            int thisId,
            int highestFloor, int thisFloorHeight,
            int loadBearing, int initHeight)
        {
            //assign the paremeters
            id = thisId;
            highestFloorCanArrive = highestFloor;//the xml file do not give the lowest floor so according to the requirement on the web the lowest is 0-floor
            capability = loadBearing;
            currentHeight = initHeight;
            floorHeight = thisFloorHeight;

            //initialize the others
            buttonPanel = null;
            freeCapability = capability;
            currentDirection = Direction.No;
            currentTargetFloor = NoFloor;
            doorOpeningTime = DoorOpeningDuration;
            isDoorOpening = false;
            isIdle = true;
            buttonPanel = new ButtonPanelInsideElev((highestFloorCanArrive+1));
        }

        //member methods
        //when a passenger come in the elevator only knows that the weight is increasing
        public bool addWeight(int weight) { 
            if(weight <= 0){
                Utility.logWarning("the passenger's weight cannot be smaller than 0");
                return false;
            }
            var tempCapability = freeCapability - weight;
            if(tempCapability < 0){
                Utility.logWarning("[Elevator"+id+"] : The elevator cannot accept new passengers otherwise it will overweight");
                return false;
            }
            freeCapability = tempCapability;
            return true;
        }
        public bool subWeight(int weight) {
            if(weight <= 0){
                Utility.logWarning("the passenger's weight cannot be smaller than 0");
                return false;
            }
            var tempCapability = freeCapability + weight;
            if (tempCapability > capability) {
                Utility.logWarning("It is impossible that this weight is contained in the elevator reasonably!");
                return false;
            }
            freeCapability = tempCapability;
            return true;
        }

        public void openDoor() { //open the door and set the current target floor as null
            Utility.log("[Elevator "+id+"] :  OPEN the door at"+CurrentFloor+" :");
            isDoorOpening = true;
            resetDoorOpeningTime();
            buttonPanel.buttonReleased(CurrentFloor);
            //trigger the  open door event
            if (doorOpenEvent != null){
                doorOpenEvent(this, null);
            } 
        }
        public void closeDoor() {//close door and assign next task as the target floor
            Utility.log("[Elevator" + id + "] :  CLOSE the door at" + CurrentFloor + " :");
            isDoorOpening = false;
            //trigger the close door event
            if (doorCloseEvent != null){
                doorCloseEvent(this, null);
            }
        }

        public bool isTargetValid(int floorNumber)
        {
            // elevator can go anywhere if it is stopped
            if (isIdle)
                return true;
            //the elevator is running to its target floor 
            if (currentTargetFloor != NoFloor)
            {
                //to find whether there are any physical limitation on the change
                if (CurrentFloor == floorNumber)
                {
                    return false;
                }
                //to predict whether the given target is in the scope of the deceleration space
                var predictFloor = (currentDirection == Direction.Up) ?
                                   (currentHeight + DecelerationSpace) / floorHeight :
                                   (currentHeight - DecelerationSpace) / floorHeight;
                var isInTheDecelerationSapce = ((currentDirection == Direction.Up) && (floorNumber <= predictFloor)) ||
                                                ((currentDirection == Direction.Down) && (floorNumber >= predictFloor));
                if (isInTheDecelerationSapce)
                {
                    return false;
                }
            }
            return true;
        }

        public bool setTargetFloor(int floorNumber) {
            if (floorNumber < 0 || floorNumber > highestFloorCanArrive) {
                Utility.logError("[Elevator"+id+"]: the elevator cannot arrive the given floor");
            }

            //the elevator have task to do
            isIdle = false;

            if (!isTargetValid(floorNumber))
                return false;

            //no matter the elevator is opening door or idle,it's easy to change its target without physical limitation
            currentTargetFloor = floorNumber;
            //decide the move direction according to target floor and current floor
            if (currentTargetFloor > CurrentFloor) {
                currentDirection = Direction.Up; 
            }
            else if (currentTargetFloor == CurrentFloor) {
                currentDirection = Direction.No; 
            }
            else if (currentTargetFloor < CurrentFloor) {
                currentDirection = Direction.Down; 
            }

            return true;
        }
        public void resetTargetFloor() {
            currentTargetFloor = NoFloor; 
        }

        public void resetDoorOpeningTime(){
            doorOpeningTime = DoorOpeningDuration; 
        }

        public void run() {
            if (isDoorOpening) {//when the elevator is opening door at its target floor
                doorOpeningTime--;
                if (doorOpeningTime == 0) {
                    closeDoor();
                }
            }
            else if (currentTargetFloor == NoFloor) {//the elevator doesn't find the next task it need to finish
                isIdle = true;
                currentDirection = Direction.No;
                Utility.log("[Elevator"+ID+"]: It is Idle");
            }
            else if (currentDirection == Direction.Down) {//if the elevator need go down 
                currentHeight -= Velocity;
                Utility.log("[Elevator" + ID + "]: now at " + CurrentFloor + " floor\t is running to " + currentTargetFloor + " floor\t Height " + currentHeight);
            }
            else if (currentDirection == Direction.Up) {//if the elevator nedd go up stairs 
                currentHeight += Velocity;
                Utility.log("[Elevator" + ID + "]: now at " + CurrentFloor + " floor\t is running to " + currentTargetFloor + " floor\t Height " + currentHeight);
            }

            //whether arrive the target floor
            if ((!isIdle) && (currentTargetFloor == CurrentFloor)) {
                openDoor();
                currentTargetFloor = NoFloor;//before next task was assigned the elevator is idle,the assignment will make "isIdle" true in the next tick's runing
            }

            //update the direction record
            historyDirection = currentDirection;
        }

        //add and remove event listener
        public void addEventListener(EventType eventType, EventHandler eventHandler){
            operationOnEventListener(eventType, eventHandler, (ref EventHandler x, ref EventHandler y) => x += y);
        }
        public void removeEventListener(EventType eventType, EventHandler eventHandler){
            operationOnEventListener(eventType, eventHandler, (ref EventHandler x, ref EventHandler y) => x -= y);
        }
        private void operationOnEventListener(EventType eventType, EventHandler eventHandler, OperationOfEventHandler operation){
            if (eventType == EventType.DoorOpen){
              operation(ref doorOpenEvent,ref eventHandler);
            }
            else if (eventType == EventType.DoorClose){
                operation(ref doorCloseEvent,ref eventHandler);
            }
        }

        public void setCurrentDirection(Direction dir)
        {
            currentDirection = dir;
            historyDirection = dir;
        }
    }
}
