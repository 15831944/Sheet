﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheet.Simulation.Core
{
    public class OrGate : Element, IStateSimulation
    {
        public OrGate() : base() { }
        public ISimulation Simulation { get; set; }
    }
}