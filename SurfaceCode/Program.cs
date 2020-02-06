// Copyright Thien Nguyen
// Licensed under the MIT License.

using Microsoft.Quantum.Samples.SurfaceCode;
using Microsoft.Quantum.Simulation.Core;
using System;
using System.Collections.Generic;
using Microsoft.Quantum.Simulation.Simulators;

namespace QEC_Sample.SurfaceCode
{
    // Callback to retrieve the next syndrome group
    // The quantum code will invoke this to get information about the next syndrome to measure.
    class GetSyndromeGroupInfoCallable : Function<QVoid, IQArray<Int64>>, ICallable, IApplyData
    {
        SurfaceCodeDatastructure m_surfaceCode;
        int m_syndromeIndex;
        // The number of syndrome measurement rounds to run.
        // In reality, this can be open-ended.
        int m_numberOfRounds;
        public GetSyndromeGroupInfoCallable(IOperationFactory m, SurfaceCodeDatastructure dataStructure, int nbRounds) : base(m)
        {
            m_surfaceCode = dataStructure;
            m_syndromeIndex = 0;
            m_numberOfRounds = nbRounds;
        }

        public override void Init() {}

        // Return a syndrom data as an integer array:
        // The first integer indicates Pauli operator: X(1), Y(2), Z(3)
        // The following items are data qubit indices.
        public override Func<QVoid, IQArray<Int64>> Body
        {
            get
            {
                return ((arg) =>
                {
                    if (m_syndromeIndex < m_surfaceCode.NSyndromes * m_numberOfRounds)
                    {
                        var syndromeQubit = m_surfaceCode.GetSyndromeQubit(m_syndromeIndex % m_surfaceCode.NSyndromes);
                        m_syndromeIndex++;
                        var syndromeDataArray = new List<Int64>();
                        if (syndromeQubit.PauliType == SurfaceCodeDatastructure.SyndromeQubit.Type.X)
                        {
                            // Pauli X
                            syndromeDataArray.Add(1);
                        }
                        else
                        {
                            // Pauli Z
                            syndromeDataArray.Add(3);
                        }

                        // Append data qubit indices
                        syndromeDataArray.AddRange(syndromeQubit.DataQubitIdx);

                        var syndromeArray = new QArray<Int64>(syndromeDataArray.ToArray());
                        return syndromeArray;
                    }
                    else
                    {
                        // Terminate the loop
                        return new QArray<Int64>();
                    }
                });
            }
        }
    }


    // Callback from quantum hardware to report a syndrom measurement result.
    // This should be invoked after GetSyndromeGroupInfoCallable was called
    // i.e. report the result for that syndrome group.
    class ReportSyndromeCallable : Function<Int64, QVoid>, ICallable, IApplyData
    {
        SurfaceCodeDatastructure m_surfaceCode;

        public ReportSyndromeCallable(IOperationFactory m, SurfaceCodeDatastructure dataStructure) : base(m)
        {
            m_surfaceCode = dataStructure;
        }

        public override void Init() { }

        public override Func<Int64, QVoid> Body
        {
            get
            {
                return ((arg) =>
                {
                    // Update surface code data-structure with the new data.
                    m_surfaceCode.AddMeasurementResult((int)arg);
                    return QVoid.Instance;
                });
            }
        }
    }

    // This is a callback to apply *additional* operations
    // (specified as a list of Pauli operators on arbitrary data qubits)
    // This will be done after a syndrome measurement is done.
    // Currently, it is only used for testing purposes, i.e. apply random X, Y, Z (and print out which one)
    // so that we can correlate with the syndome data.
    // The main purpose of this callback though is to implement *logical* operation on the surface code
    // besides the error correction cycles, e.g. adding a logical X gate (as a sequence of physical X gates) etc. 
    class ApplyRandomErrorCallable : Function<QVoid, IQArray<Int64>>, ICallable, IApplyData
    {
        /// <summary>
        /// The probability with which the error will be introduced.
        /// </summary>
        private const double errorProbability = 0.005;

        /// <summary>
        /// Random number generator used to decide when to introduce the error.
        /// </summary>
        private static readonly System.Random rnd = new System.Random();

        private int m_nbQubits;

        public ApplyRandomErrorCallable(IOperationFactory m, int nbQubits) : base(m)
        {
            m_nbQubits = nbQubits;
        }
        public override void Init() { }


        public override Func<QVoid, IQArray<Int64>> Body
        {
            get
            {
                return ((arg) =>
                {
                    if (rnd.NextDouble() < errorProbability)
                    {
                        // Single qubit error
                        var pauliSelector = rnd.Next(1, 4); // 1-3
                        var qubitSelector = rnd.Next(0, m_nbQubits);

                        var errorArray = new QArray<Int64>(new Int64[] { pauliSelector, qubitSelector });
                        return errorArray;
                    }
                    else
                    {
                        return new QArray<Int64>();
                    }
                });
            }
        }
    }

    class Host
    {
        static void Main(string[] args)
        {
            using var qsim = new QuantumSimulator();

            // Creates a distance 3 surface code array
            var surfaceCodeDs = new SurfaceCodeDatastructure(3, 3);
            Console.WriteLine("Number of required qubits = {0}", surfaceCodeDs.NQubits);
            // Run 50 rounds for testing
            var numberOfRound = 50;
            // Constructs classical callback handlers
            var surfaceCodeCallback = new GetSyndromeGroupInfoCallable(qsim, surfaceCodeDs, numberOfRound);
            var reportSyndromeCallback = new ReportSyndromeCallable(qsim, surfaceCodeDs);
            var applyErrorCallback = new ApplyRandomErrorCallable(qsim, surfaceCodeDs.NQubits);

            // Run the surface code
            RunSurfaceCodeErrorCorrectionCycle.Run(qsim, surfaceCodeDs.NQubits, surfaceCodeCallback, reportSyndromeCallback, applyErrorCallback).Wait();
        }
    }
}
