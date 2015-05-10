/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines a implement of IButtonPanel : ButtonPanel
 *      This class is the super class of ButtonPanelOutsideElev and
 *      ButtonPanelInsideElev;
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewElevatorFramework
{
    abstract class ButtonPanel : IButtonPanel
    {
        //private members
        protected readonly PanelType type;
        protected readonly int buttonCounts;
        protected bool hasNewlyPressedButton;
        protected ButtonState[] buttonDisplay;
        protected IButton[] buttons;
        protected List<IButton> allNewlyPressedButton;

        //properties
        public PanelType Type { get { return type; } }
        public int ButtonCounts { get { return buttonCounts; } }
        public bool HasNewlyPressedButton { get { return hasNewlyPressedButton; } }
        public ButtonState[] ButtonDisplay { 
            get {
                for (int i = 0; i < buttonCounts; i++) {
                    buttonDisplay[i] = buttons[i].State; 
                }
                return buttonDisplay; 
            } 
        }
        public IButton[] Buttons { get { return buttons; } }
        public List<IButton> AllNewlyPressedButton { get { return allNewlyPressedButton; } }

        //constructor
        //the initializtion of the buttons depends on specific panel
        public ButtonPanel(PanelType thisType,int buttonsQuantity) {
            type = thisType;
            hasNewlyPressedButton = false;
            allNewlyPressedButton = new List<IButton>(buttonCounts);

            buttonCounts = buttonsQuantity;
            buttons = new IButton[buttonCounts];// initialize as a set of null reference
            buttonDisplay = new ButtonState[buttonCounts];//memory been allocated
        }

        //member methods
        public void buttonPressed(int buttonIndex){
            if (buttonIndex < 0 || buttonIndex >= buttonCounts) {
                Utility.logError("the button is not exist");
                throw new ArgumentOutOfRangeException();
            }
            
            hasNewlyPressedButton = true;
            if (buttons[buttonIndex] != null) {
                buttons[buttonIndex].pressedDown();
                allNewlyPressedButton.Add(buttons[buttonIndex]);
            }
        }
        public void buttonReleased(int buttonIndex){
            if (buttonIndex < 0 || buttonIndex >= buttonCounts){
                Utility.logError("the button is not exist");
                throw new ArgumentOutOfRangeException();
            }

            if (buttons[buttonIndex] != null)
            {
                buttons[buttonIndex].beReleased();
            }
        }

        public List<int> getAllPressedButtonsIndex() {
            List<int> list = new List<int>(buttonCounts);
            for (int i = 0; i < buttonCounts; i++)
            {
                if (buttons[i].State == ButtonState.Pressed)
                {
                    list.Add(i);
                }
            }
            return list;
        }

        public void resetNewlyPressedButtonNotification() {
            hasNewlyPressedButton = false;
            allNewlyPressedButton.Clear();
        }

        public void addNewlyPressedButtonNotification(Direction dir)
        {
            allNewlyPressedButton.Add(buttons[dir == Direction.Up ? 1 : 0]);
            hasNewlyPressedButton = true;
        }

        public void removeNewlyPressedButtonNotification(Direction dir)
        {
            allNewlyPressedButton.Remove(buttons[dir == Direction.Up ? 1 : 0]);
            hasNewlyPressedButton = (allNewlyPressedButton.Count > 0);
        }

        public bool checkNewlyPressedButtonNotification(Direction dir)
        {
            return allNewlyPressedButton.Contains(buttons[dir == Direction.Up ? 1 : 0]);
        }
    }
}
