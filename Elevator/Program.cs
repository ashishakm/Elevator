using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace Elevator
{
    public enum Status : byte { pause, up, down }; //Elevator status
    public enum Order : byte { up, down, free }; //Current elevator instruction
    public enum Job : byte { up, down, free }; //Elevator current task

    public class Elevator
    {
        public Status state = Status.pause;
        public Order order = Order.free;
        public Job job = Job.free;
        //Buttons in the elevator
        public Dictionary<int, bool> destination = new Dictionary<int, bool>();
        public int currentFloor = 1; //Current floor, the first floor
        public int no; //Elevator number
        public int NoOffloor;
        public int StartingFloor;
        public Elevator()
        {
            var appSettings = ConfigurationManager.AppSettings;
            NoOffloor = Convert.ToInt32(appSettings["NoOfFloor"]);
            StartingFloor = Convert.ToInt32(appSettings["StartingFloor"]);
            for (int i = StartingFloor; i <= NoOffloor; ++i)
            {
                //Initialize elevator status: all buttons are not pressed
                destination.Add(i, false);
            }
        }

        public void Runner()
        {
            while (true)
            {
                if (state == Status.up) //Elevator is rising
                {
                    for (int i = currentFloor + 1; i <= NoOffloor; ++i)
                    {
                        if (destination[i]) //Dock
                        {
                            Console.WriteLine("elevator{0} stops at {1}", no, i);
                            state = Status.pause;
                            Console.WriteLine("elevator{0} opens its door", no);
                            destination[i] = false; //Cancel button
                            currentFloor = i; //Update Floor
                            Thread.Sleep(500); //Dock for 0.5 seconds
                            break;
                        }
                        else
                        {
                            currentFloor = i; //Update elevator floors
                            Console.WriteLine("elevator{0} arrives at {1}", no, i);
                            Thread.Sleep(500); //向下一层前进，用时0.5秒

                            int j;
                            for (j = currentFloor + 2; j <= 20; ++j) //Check if there are subsequent floors
                            {
                                if (destination[j])
                                    break;
                            }
                            if (j == NoOffloor + 1) //No subsequent floors
                            {
                                state = Status.pause;
                                order = Order.free;
                                currentFloor++;
                                Console.WriteLine("elevator{0} arrives at {1}", no, currentFloor);
                                break;
                            }
                        }
                    }
                }

                else if (state == Status.down) //The elevator is descending
                {
                    for (int i = currentFloor - 1; i >= 1; --i)
                    {
                        if (destination[i]) //Dock
                        {
                            Console.WriteLine("elevator{0} stops at {1}", no, i);
                            state = Status.pause;
                            Console.WriteLine("elevator{0} opens its door", no);
                            destination[i] = false; //Cancel button
                            currentFloor = i;
                            Thread.Sleep(500);
                            break;
                        }
                        else
                        {
                            currentFloor = i; //Update elevator floors
                            Console.WriteLine("elevator{0} arrives at {1}", no, i);
                            Thread.Sleep(500); //Advance to the next layer in 0.5 seconds

                            int j;
                            for (j = currentFloor - 2; j >= 1; --j) // Check if there are subsequent floors
                            {
                                if (destination[j])
                                    break;
                            }
                            if (j == 0) //No subsequent floors
                            {
                                state = Status.pause;
                                order = Order.free;
                                currentFloor--;
                                Console.WriteLine("elevator{0} arrives at {1}", no, currentFloor);
                                break;
                            }
                        }
                    }
                }

                else //The elevator is stopped
                {
                    if (order == Order.up) //There are still ascending orders
                    {
                        for (int i = currentFloor + 1; i <= NoOffloor; ++i)
                        {
                            if (destination[i])
                            {
                                state = Status.up;
                                break;
                            }
                        }
                        if (state == Status.pause) //There are no rising floors
                        {
                            order = Order.free; //The elevator has no instructions
                        }
                    }
                    else if (order == Order.down) //There are still descent instructions
                    {
                        for (int i = currentFloor - 1; i >= 1; --i)
                        {
                            if (destination[i])
                            {
                                state = Status.down;
                                break;
                            }
                        }
                        if (state == Status.pause) //No lower floors
                        {
                            order = Order.free; //The elevator has no instructions
                        }
                    }
                    else //There are currently no instructions
                    {
                        for (int i = 1; i <= NoOffloor; ++i)
                        {
                            if (destination[i])
                            {
                                if (i < currentFloor)
                                {
                                    order = Order.down;
                                    if (job == Job.free)
                                        job = Job.down;
                                }
                                else if (i > currentFloor)
                                {
                                    order = Order.up;
                                    if (job == Job.free)
                                        job = Job.up;
                                }
                                else //In fact this code should never be executed
                                {
                                    Console.WriteLine("elevator{0} opens its door", no);
                                    destination[i] = false; //Cancel button
                                    Thread.Sleep(500); //Dock for 0.5 seconds
                                }
                            }
                        }
                        if (order == Order.free) //No instructions
                        {
                            job = Job.free; //Elevator has no task
                            break;
                        }
                    }
                }
            }
        }

        public void Arrange(int floor) //Light up and cancel
        {
            destination[floor] = !destination[floor];
        }

        public void Display()
        {
            for (int i = 1; i <= NoOffloor; ++i)
                Console.Write("{0} ", destination[i]);
            Console.WriteLine();
        }
    }

    public class ElevatorGroup
    {

        public Elevator[] elevator = new Elevator[Convert.ToInt32(ConfigurationManager.AppSettings["NoOfElevator"])]; //Add 5 elevators
        public ElevatorGroup()
        {

            for (int i = 0; i < 5; ++i)
            {
                elevator[i] = new Elevator();
                elevator[i].no = i;
            }
        }

        public void BuildThread(int no)
        {
            ThreadStart readyElevator = new ThreadStart(elevator[no].Runner);
            Thread elevatorThread = new Thread(readyElevator);
            elevatorThread.Start();
        }

        public void Dispatch(int elevatorNo, int insideFloor) //Choose a floor inside an elevator
        {
            elevator[elevatorNo].Arrange(insideFloor);
            if (elevator[elevatorNo].destination[insideFloor] && elevator[elevatorNo].order == Order.free)
            {
                if (elevator[elevatorNo].currentFloor > insideFloor) //The elevator should go down
                {
                    elevator[elevatorNo].order = Order.down;
                    if (elevator[elevatorNo].job == Job.free) //Schedule a task
                        elevator[elevatorNo].job = Job.down;
                }
                else if (elevator[elevatorNo].currentFloor < insideFloor) //The elevator should go up
                {
                    elevator[elevatorNo].order = Order.up;
                    if (elevator[elevatorNo].job == Job.free) //Schedule a task
                        elevator[elevatorNo].job = Job.up;
                }
                else //The elevator should open the door
                {
                    Console.WriteLine("elevator{0} opens its door", elevatorNo);
                    Thread.Sleep(500); //Dock for 0.5 seconds
                    elevator[elevatorNo].Arrange(insideFloor);
                    return;
                }
                BuildThread(elevatorNo);
            }
        }

        public void Dispatch(int destinationFloor, bool goUp) //Press up or down outside the elevator, this function may cause blocking
        {
            int NoOffloor;
            var appSettings = ConfigurationManager.AppSettings;
            NoOffloor = Convert.ToInt32(appSettings["NoOfFloor"]);
            int elevatorWhoRespect = -1;
            while (elevatorWhoRespect == -1) //To prevent the lack of suitable elevators, try repeatedly until successful
            {
                if (goUp) //Go upstairs
                {
                    int distance = NoOffloor;
                    for (int i = 0; i < 5; ++i)
                    {
                        if (elevator[i].job == Job.up) //If the elevator has an ascent task
                        {
                            int currentDistance = destinationFloor - elevator[i].currentFloor;
                            if (currentDistance > 0 && currentDistance < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistance;
                            }
                            else if (currentDistance == 0 && elevator[i].state == Status.pause) //Happens to catch the elevator
                            {
                                Console.WriteLine("elevator{0} opens its door, lucky", i);
                                Thread.Sleep(500); //Dock for 0.5 seconds
                                return;
                            }
                        }
                        else if (elevator[i].order == Order.free) //If the elevator is idle
                        {
                            int currentDistanceAbs = Math.Abs(destinationFloor - elevator[i].currentFloor);
                            if (currentDistanceAbs < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistanceAbs;
                            }
                        }
                    }
                }
                else //Go downstairs
                {
                    int distance = NoOffloor;
                    for (int i = 0; i < 5; ++i)
                    {
                        if (elevator[i].job == Job.down) //If the elevator has a descent task
                        {
                            int currentDistance = elevator[i].currentFloor - destinationFloor;
                            if (currentDistance > 0 && currentDistance < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistance;
                            }
                            else if (currentDistance == 0 && elevator[i].state == Status.pause) //Happens to catch the elevator
                            {
                                Console.WriteLine("elevator{0}opens its door, lucky", i);
                                Thread.Sleep(500); //Dock for 0.5 seconds
                                return;
                            }
                        }
                        else if (elevator[i].order == Order.free) //If the elevator is idle
                        {
                            int currentDistanceAbs = Math.Abs(destinationFloor - elevator[i].currentFloor);
                            if (currentDistanceAbs < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistanceAbs;
                            }
                        }
                    }
                }

                if (elevatorWhoRespect == -1) //No suitable elevator found
                    Thread.Sleep(500);
            }
            if (goUp)
                elevator[elevatorWhoRespect].job = Job.up;
            else
                elevator[elevatorWhoRespect].job = Job.down;
            Dispatch(elevatorWhoRespect, destinationFloor);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ElevatorGroup elevatorGroup = new ElevatorGroup();
            string control;

            while (true)
            {
                Console.WriteLine("Enter Test case NO: 1 / 2 / 3 / 4 :");
                control = Console.ReadLine();
                switch (control)
                {
                    case "1":
                        /*Test 1: Key press inside the elevator*/
                        elevatorGroup.Dispatch(2, 5);
                        elevatorGroup.Dispatch(2, 12);
                        elevatorGroup.Dispatch(2, 20);
                        elevatorGroup.Dispatch(2, 15);
                        while (elevatorGroup.elevator[2].currentFloor != 13) //Execute after reaching the 13th floor
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(2, 7);
                        while (elevatorGroup.elevator[2].currentFloor != 7) //Execute after reaching the 7th floor
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(2, 5);
                        while (elevatorGroup.elevator[2].currentFloor != 5)
                            Thread.Sleep(200);
                        Thread.Sleep(500);
                        elevatorGroup.Dispatch(2, 5);
                        elevatorGroup.Dispatch(2, 9);
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    case "2":
                        /*Test 2: Complex key press outside the elevator*/
                        for (int i = 2; i <= 7; ++i)
                        {
                            elevatorGroup.Dispatch(i, false);
                            elevatorGroup.Dispatch(i, true);
                        }
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    case "3":
                        /*Test 3: Cancel the button function (inside the elevator)*/
                        elevatorGroup.Dispatch(2, 15); // Going to the 15th floor
                        while (elevatorGroup.elevator[2].currentFloor != 6) //Reached the 6th floor
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(2, 15); //Cancel button
                        Thread.Sleep(1000); //Hesitate for 1 second
                        elevatorGroup.Dispatch(2, 1); //Back to the first floor
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    case "4":
                        /*Test 4: Mixed tasks in different directions*/
                        elevatorGroup.Dispatch(1, true); //   1st floor up
                        elevatorGroup.Dispatch(0, 20); //Elevator 0 is going to the 20th floor
                        Thread.Sleep(500);
                        elevatorGroup.Dispatch(6, true); //6th floor up, elevator 0 should stop
                        elevatorGroup.Dispatch(9, false); //9th floor down, elevator 0 should not stop
                        elevatorGroup.Dispatch(1, 1);
                        while (elevatorGroup.elevator[0].currentFloor != 15) //To the 15th floor
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(0, 20); //Lift 0 cancels the 20th floor
                        Thread.Sleep(1000);
                        elevatorGroup.Dispatch(0, 2); //Elevator 0 goes to the second floor
                        while (elevatorGroup.elevator[0].currentFloor != 14) //To the 14th floor
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(13, false); //On the 13th floor, lift 0 should stop
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    default:
                        return;
                }
            }
        }
    }
}
