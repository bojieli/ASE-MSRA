/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines all the enums and interfaces used in the entire solution
 **/
using System;
using System.Collections.Generic;

namespace NewElevatorFramework
{
    /*----------------------------define the enums----------------------------*/
    public enum DoorState { 
        Closed=0,//these will be the initial value
        Opened
    }
    public enum ButtonState { 
        Pressed,
        Released
    }
    //ButtonType is the type to distinguish buttons inside Elvator as floor button
    //   from the buttons  outside elevator as direction button
    public enum ButtonType { 
        DirectionButton,//generally, this type button is outside the elevator
        FloorButton     // this type usually inside the elevator
    }
    public enum PanelType { 
        OutsideElevatorPanel,
        InsideElevatorPanel
    }
    public enum EventType { 
        DoorClose,
        DoorOpen
    }
    public enum TypeOfButtonOutside { //this type use for the Direction Button
        DownButton = 0,
        UpButton = 1
    }
    public enum IndexOfOutsidButton
    { //the value is used for the array stored the buttons
        Down = 0,
        Up = 1
    }
    public enum Direction { 
        Down,
        No,
        Up
    }
    public enum MotionOfElevator { 
        Velocity = 2,
        DecelerationSpace = 5
    }
    /*----------------------------define the interface----------------------------*/
    public interface IButton {
        int IndexValueInPanel { get; }
        ButtonType Type { get; }
        ButtonState State { get; }
        void pressedDown();
        void beReleased();
    }
    //buttonDisplay,buttonPressed,buttonReleased, these thress members are accessable to passengers
    //others should used by scheduler
    public interface IButtonPanel {
        //properties
        PanelType Type { get; }
        int ButtonCounts { get; }
        bool HasNewlyPressedButton { get; }
        ButtonState[] ButtonDisplay { get; }   //to record the displayed state of the button panel user can see 
        IButton[] Buttons { get; }             //to save the button objects in the button panel
        List<IButton> AllNewlyPressedButton { get; }
        //methods
        void buttonPressed(int buttonIndex);
        void buttonReleased(int buttonIndex);
        List<int> getAllPressedButtonsIndex();
        void resetNewlyPressedButtonNotification(); 
    }
    public interface IElevator {
        //properties
        int ID { get; }
        IButtonPanel ButtonPanel { get; }
        int Capability { get; }
        int FreeCapability { get; }
        int HighestFloorCanArrive { get; }
            //current state
        Direction CurrentDirection { get; }
        Direction HistoryDirection { get; }
        int CurrentHeight { get; }
        int CurrentTargetFloor { get; }
        int CurrentFloor { get; }
        bool IsDoorOpening { get; }
        bool IsIdle { get; }
        bool IsEmpty { get; }
        //methods
        void addEventListener(EventType eventType, EventHandler eventHandler);
        void removeEventListener(EventType eventType, EventHandler eventHandler);
        bool addWeight(int weight);
        bool subWeight(int weight);
        void openDoor();
        void closeDoor();
        bool isTargetValid(int floorNumber);
        bool setTargetFloor(int floorNumber);
        void run();
        void setCurrentDirection(Direction dir);
    }
}