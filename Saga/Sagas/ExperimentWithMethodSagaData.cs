using System;
using System.Collections.Generic;
using System.Linq;

namespace TestPlanningSaga.Sagas
{
    public class ExperimentWithMethodSagaData
    {
        public ExperimentData Experiment;
        public List<MethodData> Methods;
        public string failureReason;
        public long ExperimentId => Experiment.Id;
        public List<long> MethodsIds => Methods.Select(m => m.Id).ToList();

        public bool MethodsAddedToExperiment;
        public bool ExperimentAddedToMethods;
    }

    public class ExperimentData
    {
        public long Id { get; set; }

        public string Creator { get; set; }

        public string Name { get; set; }

        public DateTime CreationDate { get; set; }
    }

    public class MethodData
    {
        public long Id { get; set; }

        public string Creator { get; set; }

        public string Name { get; set; }

        public decimal ApplicationRate { get; set; }

        public DateTime CreationDate { get; set; }
    }
}
