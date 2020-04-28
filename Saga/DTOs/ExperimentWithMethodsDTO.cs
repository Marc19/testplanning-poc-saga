using System;
using System.Collections.Generic;

namespace TestPlanningSaga.DTOs
{
    public class ExperimentWithMethodsDTO
    {
        public ExperimentDTO Experiment { get; set; }

        public List<MethodDTO> Methods { get; set; }
    }

    public class ExperimentDTO
    {
        public string Creator { get; set; }

        public string Name { get; set; }
    }

    public class MethodDTO
    {
        public string Creator { get; set; }

        public string Name { get; set; }

        public decimal ApplicationRate { get; set; }
    }
}
