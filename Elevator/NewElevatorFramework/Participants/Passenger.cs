/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines the passenger and his operations;
 *      It's clear that the passenger only has four thing to
 *      do while the whole simulation: 
 *      1.arrive and press up/down to get the elevators to carry him;
 *      2.enter a elevator if the elevator can carry him;
 *      3.press button inside elevator so that the elevator can carry him
 *          to the target floor.
 *      4.leave the elevator when him arrive the target floor
 *      
 *      There is no interface for the passenger since neither the elevators
 *      and scheduler know there is a passenger: 
 *          elevator can only detect the changes of the weight inside it;
 *          scheduler detect changes by the button panels' state.
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewElevatorFramework
{
    class Passenger
    {
        //private member variables
        string name;
        int originalFloor;
        int targetFloor;
        int comingTime;
        int arrivedTime;
        int weight;
        bool isInsideTheElev;
        bool isArrived;
        Direction direction;
        IElevator elevatorStayedIn;
        //properties
        public string Name { get { return name; } }
        public int OriginalFloor { get { return originalFloor; } }
        public int TargetFloor { get { return targetFloor; } }
        public int ComingTime { get { return comingTime; } }
        public int ArrivedTime { get { return arrivedTime; } }
        public int Weight { get { return weight; } }
        public bool IsInsideTheElev { get { return isInsideTheElev; } }
        public bool IsArrived { get { return isArrived; } }
        public bool IsTargetButtonInsideElevPressed {
            get {
                if (elevatorStayedIn == null) { 
                    return false; 
                }
                if (elevatorStayedIn.ButtonPanel.ButtonDisplay[targetFloor] == ButtonState.Released) { 
                    return false; 
                }
                return true;
            } 
        }
        public Direction DirectionOfMotion { get { return direction; } }
        public IElevator ElevatorStayedIn { get { return elevatorStayedIn; } }

        //constructor
        public Passenger(
            string passengerName, int passengerComingTime, 
            int passengerFromFloor, int passengerTargetFloor, 
            int PassegerWeight) 
        {
            name = passengerName;
            comingTime = passengerComingTime;
            originalFloor = passengerFromFloor;
            targetFloor = passengerTargetFloor;
            weight = PassegerWeight;
            //set the default properties
            arrivedTime = 0;
            isInsideTheElev = false;
            isArrived = false;
            if (targetFloor > originalFloor) {
                direction = Direction.Up; 
            }
            else if (targetFloor < originalFloor){
                direction = Direction.Down;
            }
            else {
                direction = Direction.No;
                Utility.logWarning("Here is a strange passenger : "+name+", he has no direction to go");
                isArrived = true;
            }
        }

        //member methods
        public bool enterElevator(IElevator comingElev)
        {
            if (!comingElev.IsDoorOpening)
            {
                Utility.logWarning("passenger cannot enter a unavailable elevator  :   door does not open");
                return false;
            }

            if (comingElev.addWeight(weight))
            {//passenger enter the elevator successfully 
                isInsideTheElev = true;
                elevatorStayedIn = comingElev;
                //press the target floor button in the elevator
                if (comingElev.ButtonPanel.ButtonDisplay[targetFloor] == ButtonState.Released)
                {
                    pressButtonInsideElev(comingElev.ButtonPanel);
                }
                Utility.log("=>"+Name + " enter Elevator" + comingElev.ID);
                return true;
            }
            Utility.logWarning("***The elevator"+comingElev.ID+" is full,passenger "+name+" enter action failed***");
            return false;
        }
        public bool leaveElevator(int leaveTick)
        {
            if (!isInsideTheElev)
            {
                Utility.logWarning("The passenger "+name+" is not in a elevator,leave elevator operation is unavailable");
                return false;
            }
            if (leaveTick < 0) {
                Utility.logWarning("The passenger "+name+" give notice : your watch may be wrecked");
                return false;
            }

            if (elevatorStayedIn.subWeight(weight))
            {
                Utility.log("=>"+Name+" leave Elevator"+elevatorStayedIn.ID);
                isInsideTheElev = false;
                isArrived = true;
                elevatorStayedIn = null;
                arrivedTime = leaveTick;
                return true;
            }
            return false;
        }

        public void pressButtonInsideElev(IButtonPanel buttonPanel) {
            if (buttonPanel == null) {
                Utility.logError("Wrong argument : Passenger::pressButtonInsideElev");
                throw new ArgumentNullException();
            }
            buttonPanel.buttonPressed(targetFloor);
        }
        public void pressButtonOutsideElev(IButtonPanel buttonPanel) {
            if (buttonPanel == null) {
                Utility.logError("Wrong argument");
                throw new ArgumentNullException();
            }
            //press the specifical button
            if (direction == Direction.Down) {
                buttonPanel.buttonPressed((int)IndexOfOutsidButton.Down);
            }
            else if (direction == Direction.Up)
            {
                buttonPanel.buttonPressed((int)IndexOfOutsidButton.Up);
            }
            else {
                Utility.logWarning("What is the passenger thinking?!");
                return;
            }
        }
    }
}
