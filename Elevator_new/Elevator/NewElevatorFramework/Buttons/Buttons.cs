/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines the specific buttons as up/down button 
 *      pressed outside and floor buttons(1,2,3,...) inside elevator
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewElevatorFramework
{
    //interface IButton only show the public interface 
    // we should implement a concrete class
    abstract class Button : IButton
    { 
        //private members
        protected int indexValueInPanel;
        protected ButtonState state;
        protected ButtonType type;
        //properties
        public int IndexValueInPanel { get { return indexValueInPanel; } }
        public ButtonType Type { get { return type; } }
        public ButtonState State { get { return state; } }

        //constructor
        public Button(ButtonType thisType) {
            type = thisType;
            state = ButtonState.Released;
        }

        //member methods
        public void pressedDown() {
            state = ButtonState.Pressed; 
        }
        public void beReleased() {
            state = ButtonState.Released; 
        }
    }

    class FloorButtonInsideElev : Button
    {
        //constructor
        public FloorButtonInsideElev(int floorValue) :base(ButtonType.FloorButton){
            indexValueInPanel = floorValue;//the value shown on the button i.e. 12(represent 12F)
        }

        //member methods
    }

    class DirectionButtonOutsideElev : Button
    {
        //private members
        TypeOfButtonOutside specificType;//value shown on the button 

        //properties
        public TypeOfButtonOutside SpecificType { get { return specificType; } }
        
        //constructor
        public DirectionButtonOutsideElev(TypeOfButtonOutside buttonValue):base(ButtonType.DirectionButton) {
            specificType = buttonValue;
            if (buttonValue == TypeOfButtonOutside.DownButton) {
                indexValueInPanel = (int)IndexOfOutsidButton.Down;
            }
            else if (buttonValue == TypeOfButtonOutside.UpButton) {
                indexValueInPanel = (int)IndexOfOutsidButton.Up; 
            }
        }

        //member methods
    }
}
