/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file is the entrance of this solution
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NewElevatorFramework
{
    class SimulateProgram
    {
        IElevator[] elevators;
        Passenger[] passengers;
        Scheduler elevatorScheduler;
        int globalTickClock;
        bool simulationOver = false;
        //properties
        public IElevator[] Elevators { get { return elevators; } }
        public Passenger[] Passengers { get { return passengers; } }
        public Scheduler ElevatorScheduler { get { return elevatorScheduler; } }

        //constructor
        public SimulateProgram()
        {
            globalTickClock = 0;

        }

        //member methods
        bool loadInfoFromFiles(string elevatorsInfoFile, string passengersInfoFile)
        {
            if (elevatorsInfoFile == "" || passengersInfoFile == "")
            {
                Utility.logError("Simulation initial time : no simulate data");
            }
            elevators = loadElevatorsFromFile(elevatorsInfoFile);
            passengers = loadPassengersFromFile(passengersInfoFile);
            if (elevators == null || passengers == null)
            {
                return false;
            }
            return true;
        }
        //specific load methods
        IElevator[] loadElevatorsFromFile(string elevatorsInfoFile)
        {
            if (elevatorsInfoFile == "")
            {
                Utility.logError("Simulation initial time : no elevator information File Name");
            }
            List<IElevator> elevatorList = new List<IElevator>();
            Loader.ElevatorLoader elevatorLoader = new Loader.ElevatorLoader(elevatorsInfoFile);
            //load from file
            Loader.Elevators elevatorsData = elevatorLoader.Load();
            //use the data to generate objects
            foreach (var dataObject in elevatorsData.elevator)
            {
                Elevator obj = new Elevator(
                    dataObject.id,
                    dataObject.highestfloor,
                    dataObject.floorheight,
                    dataObject.capability,
                    dataObject.initheight
                    );
                elevatorList.Add(obj);
            }
            return elevatorList.ToArray();
        }
        Passenger[] loadPassengersFromFile(string passengersInfoFile)
        {
            if (passengersInfoFile == "")
            {
                Utility.logError("Simulation initial time : no  passenger information File Name");
            }
            List<Passenger> passengerList = new List<Passenger>();
            Loader.PassengerLoader passengerLoader = new Loader.PassengerLoader(passengersInfoFile);
            //load from file
            Loader.Passengers passengerData = passengerLoader.Load();
            //use the data to generate objects
            foreach (var dataObject in passengerData.passenger)
            {
                Passenger obj = new Passenger(
                    dataObject.name,
                    dataObject.comingtime,
                    dataObject.fromfloor,
                    dataObject.tofloor,
                    dataObject.weight
                    );
                passengerList.Add(obj);
            }
            return passengerList.ToArray();
        }

        bool initScheduler()
        {
            if (elevators == null)
            {
                Utility.logError("Simulation initial time : initialize elevators had not been finished!");
                return false;
            }
            int floorNum = 0;
            foreach (var elev in elevators)
            {
                floorNum = Math.Max(floorNum, elev.HighestFloorCanArrive);
            }

            elevatorScheduler = new Scheduler(floorNum + 1);//the floor 0
            elevatorScheduler.bindWithElevators(elevators);
            return true;
        }

        void elevatorsRun()
        {
            foreach (var elev in elevators)
            {
                elev.run();
            }
        }

        void passengerTakeActions()
        {
            bool isSimulationOver = true;
            foreach (var passenger in passengers)
            {
                isSimulationOver &= passenger.IsArrived; //once there is a passenger doesn't arrive, the siSimulatoinOver will become false

                if (passenger.IsArrived)
                {//omit the arrive passengers
                    continue;
                }

                //if passenger is just coming ,record it
                if (passenger.ComingTime == globalTickClock)
                {
                    Utility.log("Passenger[" + passenger.Name + "] is coming to floor "
                        + passenger.OriginalFloor + " target " + passenger.TargetFloor);
                    //check
                    if (!elevatorScheduler.checkFloor(passenger.OriginalFloor) || 
                        !elevatorScheduler.checkFloor(passenger.TargetFloor)
                        ) 
                    {
                        Utility.logError("passenger["+passenger.Name+"] want go "+
                                         "from floor "+passenger.OriginalFloor+
                                         " to floor"+passenger.TargetFloor+
                                         " ,This is imporssible !");
                        return;
                    }
                    
                }

                //whether the passenger is appeared during the global time scope
                if (passenger.ComingTime <= globalTickClock)
                {
                    //the passenger is inside the elevator
                    if (passenger.IsInsideTheElev)
                    {
                        actionsInsideElevator(passenger);
                    }
                    else
                    { //the passenger is outside the elevator
                        actionsOutsideElevator(passenger);
                    }
                }
            }
            simulationOver = isSimulationOver;//simulator.simulationOver determine whethr the simulation is over
        }

        void actionsInsideElevator(Passenger someOne)
        {
            if (someOne == null)
            {
                throw new ArgumentNullException();
            }
            IElevator hisElev = someOne.ElevatorStayedIn;
            if (hisElev.IsDoorOpening && hisElev.CurrentFloor == someOne.TargetFloor)
            {
                someOne.leaveElevator(globalTickClock);
                return;
            }
        }
        void actionsOutsideElevator(Passenger someOne)
        {
            if (someOne == null)
            {
                throw new ArgumentNullException();
            }
            DoorState[] elevatorsDoorState = elevatorScheduler.DefenceDoorState[someOne.OriginalFloor];
            ButtonPanelOutsideElev buttonPanel = elevatorScheduler.DirectionButtons[someOne.OriginalFloor];
            int hisDirection = (someOne.DirectionOfMotion == Direction.Down) ?
                               ((int)IndexOfOutsidButton.Down) :
                               ((int)IndexOfOutsidButton.Up);

            //whether the there are elevator arrived
            for (int i = 0; i < elevatorsDoorState.Length; i++)
            {
                if (elevatorsDoorState[i] == DoorState.Opened)
                {
                    //** pay attention here decide why a passenger get into one elevator
                    //the passenger make decision by the history direction of elevators
                    if (someOne.DirectionOfMotion == elevators[i].HistoryDirection)
                    {
                        if (someOne.enterElevator(elevators[i]))
                        {
                            return;//the passenger is no longer at outside
                        }//if enter failed ,try another elevator
                    }
                }
            }

            //whether the direction button is pressed
            if (buttonPanel.ButtonDisplay[hisDirection] == ButtonState.Released)
            {
                someOne.pressButtonOutsideElev(buttonPanel);
            }
        }

        void schedulerDepatchTasks()
        {
            elevatorScheduler.despatchQueriesToElev();
        }


        //Main functions
        static void Main(string[] args)
        {
            Thread logFileSave = null;
            //arguments check
            if (args.Count() != 2)
            {
                Console.WriteLine("usage:");
                Console.WriteLine("\tworld.exe elevators.xml passengers.xml");
                Console.ReadKey();
                return;
            }

            //initialize 
            SimulateProgram simulator = new SimulateProgram();
            if (!simulator.loadInfoFromFiles(args[0], args[1]))
            {
                Utility.logError("Simulation initial time : Cannot retrieve informations from file!");
                return;
            }
            if (!simulator.initScheduler())
            {
                Utility.logError("Simulation initial time : Initialize scheduler failed!");
                return;
            }

            //simulation
            while (!simulator.simulationOver)
            {
                //global clock run
                simulator.globalTickClock++;
                Utility.log("#Tick " + simulator.globalTickClock + "--------------------");
                //detect passengers' actions
                simulator.passengerTakeActions();
                //scheduler despatch the request to specifical elevator
                simulator.schedulerDepatchTasks();
                //run the elevators
                simulator.elevatorsRun();
                Utility.log("\n");
            }

            //output information
            Console.WriteLine("Simulation finish time:{0}", simulator.globalTickClock);
            Utility.outputAnalysisResult(simulator);
            logFileSave = Utility.saveLogRecord("DebugElevatorLog.txt");
            Console.ReadKey();
            if (logFileSave != null) {
                logFileSave.Join();
            }
        }
    }
}
