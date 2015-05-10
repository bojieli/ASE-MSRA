/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines two specifical button panels, they are:
 *      Class ButtonPanelOutSideElev: the button panel contains the
 *          go up button and go down button ,and when the passenger
 *          arrives he will press only of the two buttons so that the
 *          elevator will stop to carry him to his target floor;
 *      Class ButtonPanelInsideElev: the button panel is inside the 
 *          elevator , the buttons on the panel are 1,2,3... represent
 *          the target floor passengers want to go.
 *          passengers will press button in the panel when the elevator
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewElevatorFramework
{
    class ButtonPanelOutsideElev : ButtonPanel
    {
        //private members
        int floorLocation;
        //properties
        public int FloorLocation { get { return floorLocation; } }
        //constructor
        public ButtonPanelOutsideElev(int floorValue) : base(PanelType.OutsideElevatorPanel,2) {//2 means two buttons : up button and down button
            floorLocation = floorValue;
            buttons[0] = new DirectionButtonOutsideElev( TypeOfButtonOutside.DownButton);
            buttons[1] = new DirectionButtonOutsideElev( TypeOfButtonOutside.UpButton);
        }
    }

    class ButtonPanelInsideElev : ButtonPanel 
    { 
        //constructor
        //according to the elevator xml file we assume that the elevaotr's lowest floor are 0-floor for simply
        public ButtonPanelInsideElev(int buttonQuantity):base(PanelType.InsideElevatorPanel,buttonQuantity){
            for (int i = 0; i < buttonCounts; i++) {
                buttons[i] = new FloorButtonInsideElev(i);
            }
        }
    }
}
