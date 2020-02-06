// Copyright (c) Thien Nguyen.
// Licensed under the MIT License.

using System;

using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Core;
using System.Runtime.InteropServices;

namespace Microsoft.Quantum.Samples.SimulatorWithOverrides
{
    /// <summary>
    /// A simulator which extends QuantumSimulator and redefines measurement operation 
    /// to introduce a bit-flip error happening before measurement with certain probability.
    /// </summary>
    public class FaultySimulator : QuantumSimulator
    {

        [DllImport(QSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "X")]
        private static extern void ApplyOriginalX(uint id, uint qubit);

        [DllImport(QSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCX")]
        private static extern void ApplyOriginalCX(uint id, uint count, uint[] ctrls, uint qubit);

        /// <summary>
        /// The probability with which the error will be introduced.
        /// </summary>
        public static double flipProbability = 0.01;

        /// <summary>
        /// Random number generator used to decide when to introduce the error.
        /// </summary>
        private static readonly System.Random rnd = new System.Random();


        public FaultySimulator() : base(throwOnReleasingQubitsNotInZeroState: false)
        { }

        private static void ApplyRandomNoiseOp(Qubit qubit)
        {
            // Introduce the X error with certain probability
            if (rnd.NextDouble() < flipProbability)
            {
                // HACK: we know that we only has 1 simulator :) hence use Id 0
                ApplyOriginalX(0, (uint)qubit.Id);
            }
        }

        private static void ApplyRandomNoiseCNOT(Qubit ctrlQubit, Qubit targetQubit)
        {
            // Introduce error:
            // IX, XI, XX: each with probability flipProbability/3
            // Otherwise, CNOT
            if (rnd.NextDouble() < flipProbability)
            {
                var selector = rnd.Next(1, 4); // 1 to 3
                if (selector == 1)
                {
                    ApplyOriginalX(0, (uint)ctrlQubit.Id);
                }
                else if (selector == 2)
                {
                    ApplyOriginalX(0, (uint)targetQubit.Id);
                }
                else if (selector == 3)
                {
                    ApplyOriginalX(0, (uint)ctrlQubit.Id);
                    ApplyOriginalX(0, (uint)targetQubit.Id);
                }
            }
            else
            {
                // No error, let's apply CNOT
                uint[] ctrlIdx = { (uint)ctrlQubit.Id };
                ApplyOriginalCX(0, 1, ctrlIdx, (uint)targetQubit.Id);
            }
        }

        /// <summary>
        /// The overriding definition for operation M
        /// </summary>
        public class M : QSimM
        {

            private FaultySimulator Simulator { get; }

            public M(FaultySimulator m) : base(m) { }

            /// <summary>
            /// The actual definition of what the new operation does.
            /// </summary>
            public override Func<Qubit, Result> Body
            {
                get
                {
                    // Get the original M operation to call it and process the results
                    Func<Qubit, Result> originalMeasurementOperation = base.Body;

                    // The body of the operation is a lambda
                    return (qubit =>
                    {
                        ApplyRandomNoiseOp(qubit);

                        // Call the original (perfect) M operation to get final measurement results.
                        // Q# type Result which denotes measurement results maps to C# type with the same name
                        return originalMeasurementOperation(qubit);
                    });
                }
            }
        }

        /// <summary>
        /// The overriding definition for X and CNOT (CX)
        /// </summary>
        public class X : QSimX
        {

            private FaultySimulator Simulator { get; }

            public X(FaultySimulator m) : base(m) { }

            public override Func<Qubit, QVoid> Body
            {
                get
                {
                    // Get the original X operation to call it 
                    Func<Qubit, QVoid> originalXOperation = base.Body;

                    // The body of the operation is a lambda
                    return (qubit =>
                    {
                        // For a bit flip-only model, this seems to be inefficient,
                        // i.e. apply X-X back-to-back.
                        // But we keep it this way so that we can update the noise model easily.
                        ApplyRandomNoiseOp(qubit);

                        // Call the original (perfect) X operation 
                        return originalXOperation(qubit);
                    });
                }
            }


            public Func<(IQArray<Qubit>, Qubit), QVoid> NoopControlledBody => (args) =>
            {
                return QVoid.Instance;
            };

            public override Func<(IQArray<Qubit>, Qubit), QVoid> ControlledBody
            {
                get
                {
                    return (args =>
                    {
                        var (ctrls, q1) = args;
                        if (ctrls.Count == 1)
                        {
                            // It's a CNOT (single control)
                            // Let our faulty simulator handle it :-)
                            ApplyRandomNoiseCNOT(ctrls[0], q1);

                            return NoopControlledBody(args);

                        }
                        else
                        {
                            // Let the base handle it
                            return base.ControlledBody(args);
                        }
                    });
                }

            }
        }

        /// <summary>
        /// The overriding definition for operation H
        /// </summary>
        public class H : QSimH
        {

            private FaultySimulator Simulator { get; }

            public H(FaultySimulator m) : base(m) { }

            /// <summary>
            /// The actual definition of what the new operation does.
            /// </summary>
            public override Func<Qubit, QVoid> Body
            {
                get
                {
                    // Get the original H operation to call it 
                    Func<Qubit, QVoid> originalHadamardOperation = base.Body;

                    // The body of the operation is a lambda
                    return (qubit =>
                    {
                        ApplyRandomNoiseOp(qubit);

                        // Call the original (perfect) H operation 
                        return originalHadamardOperation(qubit);
                    });
                }
            }
        }
    }
}
