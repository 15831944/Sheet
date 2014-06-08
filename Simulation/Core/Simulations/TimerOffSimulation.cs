﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheet.Simulation.Core
{
    public class TimerOffSimulation : ISimulation
    {
        #region Constructor

        public TimerOffSimulation()
            : base()
        {
            this.InitialState = false;
        }

        #endregion

        #region ISimulation

        public Element Element { get; set; }

        public IClock Clock { get; set; }

        public IBoolState State { get; set; }
        public bool? InitialState { get; set; }
        public Tuple<IBoolState, bool>[] StatesCache { get; set; }
        public bool HaveCache { get; set; }

        public Element[] DependsOn { get; set; }

        public void Compile()
        {
            if (HaveCache)
                Reset();

            // only one input is allowed for timer
            var inputs = Element.Children.Cast<Pin>().Where(pin => pin.Connections != null && pin.Type == PinType.Input);

            if (inputs == null || inputs.Count() != 1)
            {
                if (SimulationSettings.EnableDebug)
                {
                    Debug.Print("No Valid Input for Id: {0} | State: {1}", Element.ElementId, State.State);
                }

                return;
            }

            // get all connected inputs with possible state
            var connections = inputs.First().Connections.Where(x => x.Item1.Type == PinType.Output);

            // set ISimulation dependencies (used for topological sort)
            DependsOn = connections.Where(x => x.Item1 != null).Select(y => y.Item1.SimulationParent).Take(1).ToArray();

            if (SimulationSettings.EnableDebug)
            {
                foreach (var connection in connections)
                {
                    Debug.Print("Pin: {0} | Inverted: {1} | SimulationParent: {2} | Type: {3}",
                    connection.Item1.ElementId,
                    connection.Item2,
                    (connection.Item1.SimulationParent != null) ? connection.Item1.SimulationParent.ElementId : UInt32.MaxValue,
                    connection.Item1.Type);
                }
            }

            // get all connected inputs with state
            var states = connections.Select(x => new Tuple<IBoolState, bool>((x.Item1.SimulationParent as IStateSimulation).Simulation.State, x.Item2)).ToArray();

            if (states.Length == 1)
            {
                StatesCache = states;
                HaveCache = true;
            }
            else
            {
                // invalidate state
                State = null;

                StatesCache = null;
                HaveCache = false;
            }
        }

        private bool IsEnabled;
        private bool IsLowEnabled;
        private long EndCycle;

        public void Calculate()
        {
            if (HaveCache)
            {
                // calculate new state
                var first = StatesCache[0];
                bool? enableState = first.Item2 ? !(first.Item1.State) : first.Item1.State;

                switch (enableState)
                {
                    case true:
                        {
                            if (IsEnabled == false && IsLowEnabled == false)
                            {
                                State.State = true;
                                IsEnabled = true;
                                IsLowEnabled = false;
                            }
                            else if (IsEnabled == true && IsLowEnabled == true && State.State != false)
                            {
                                if (Clock.Cycle >= EndCycle)
                                {
                                    State.State = false;
                                    IsEnabled = false;
                                    IsLowEnabled = false;
                                    break;
                                }
                            }
                        }
                        break;
                    case false:
                        {
                            if (IsEnabled == true && IsLowEnabled == false)
                            {
                                // Delay -> in seconds
                                // Clock.Cycle
                                // Clock.Resolution -> in milsisecond
                                long cyclesDelay = (long)((Element as ITimer).Delay * 1000) / Clock.Resolution;
                                EndCycle = Clock.Cycle + cyclesDelay;

                                IsLowEnabled = true;
                                break;
                            }
                            else if (IsEnabled == true && IsLowEnabled == true && State.State != false)
                            {
                                if (Clock.Cycle >= EndCycle)
                                {
                                    State.State = false;
                                    IsEnabled = false;
                                    IsLowEnabled = false;
                                    break;
                                }
                            }
                        }
                        break;
                    case null:
                        {
                            IsEnabled = false;
                            IsLowEnabled = false;
                            State.State = null;
                        }
                        break;
                }

                if (SimulationSettings.EnableDebug)
                {
                    Debug.Print("Id: {0} | State: {1}", Element.ElementId, State.State);
                    Debug.Print("");
                }
            }
        }

        public void Reset()
        {
            HaveCache = false;
            StatesCache = null;
            State = null;
            Clock = null;
        }

        #endregion
    }
}