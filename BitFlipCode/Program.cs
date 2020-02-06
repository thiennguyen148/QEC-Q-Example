// Copyright (c) Thien Nguyen
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Quantum.Samples.SimulatorWithOverrides;
using Microsoft.Quantum.Simulation.Core;

namespace BitFlipCode
{
    class Program
    {
        // Command line arguments:
        //  --flip-probability: bit flip probability
        //  --n-shots: number of shots to run
        //  --model: Single, Encoded, Concatenate
        //  --test-case: Measure, Bell
        static int Main()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            double flipProb = 0.0;
            Int64 nbShots = 1000;
            string model = "";
            string testCase = "";

            for (int i = 1; i < arguments.Length; ++i)
            {
                if (arguments[i] == "--flip-probability")
                {
                    if (i + 1 < arguments.Length)
                    {
                        var flipProbStr = arguments[i+ 1];
                        i++;
                        if (!Double.TryParse(flipProbStr, out flipProb))
                        {
                            Console.WriteLine("Unable to parse '{0}' as double.", flipProbStr);
                        }
                    }
                    else
                    {
                        Console.WriteLine("--flip-probability requires at least one parameter\n");
                        return -1;
                    }
                }

                if (arguments[i] == "--n-shots")
                {
                    if (i + 1 < arguments.Length)
                    {
                        var nbShotsStr = arguments[i + 1];
                        i++;
                        if (!Int64.TryParse(nbShotsStr, out nbShots))
                        {
                            Console.WriteLine("Unable to parse '{0}' as integer.", nbShotsStr);
                        }
                    }
                    else
                    {
                        Console.WriteLine("--n-shots requires at least one parameter\n");
                        return -1;
                    }
                }

                if (arguments[i] == "--model")
                {
                    if (i + 1 < arguments.Length)
                    {
                        model = arguments[i+ 1];
                        i++;
                        if (model != "Single" && model != "Encoded" && model != "Concatenate")
                        {
                            Console.WriteLine("Invalid --model parameter.\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("--model requires at least one parameter\n");
                        return -1;
                    }
                }

                if (arguments[i] == "--test-case")
                {
                    if (i + 1 < arguments.Length)
                    {
                        testCase = arguments[i + 1];
                        i++;
                        if (testCase != "Measure" && testCase != "Bell")
                        {
                            Console.WriteLine("Invalid --test-case parameter.\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("--test-case requires at least one parameter\n");
                        return -1;
                    }
                }
            }

            if (flipProb < 0.0 || flipProb > 1.0 || testCase.Length == 0 || model.Length == 0)
            {
                Console.WriteLine("Invalid parameters detected. Please check your inputs.\n");
                return -1;
            }

            using var qsim = new FaultySimulator();
            FaultySimulator.flipProbability = flipProb;

            if (testCase == "Measure")
            {
                if (model == "Single")
                {
                    var result = DoFaultyMeasurements.Run(qsim, nbShots).Result;
                    Console.WriteLine("Result = {0}", result);
                    return 0;

                }
                else if (model == "Encoded")
                {
                    var result = DoFaultyMeasurementsBitFlipCode.Run(qsim, nbShots).Result;
                    Console.WriteLine("Result = {0}", result);
                    return 0;
                }
                else
                {
                    var result = DoFaultyMeasurementsBitFlipConcatenateCode.Run(qsim, nbShots).Result;
                    Console.WriteLine("Result = {0}", result);
                    return 0;
                }
            }
            else
            {
                if (model == "Single")
                {
                    var result = DoBellFaultyMeasurements.Run(qsim, nbShots).Result;
                    Console.WriteLine("Result = {0}", result);
                    return 0;
                }
                else if (model == "Encoded")
                {
                    var result = DoBellFaultyMeasurementsBitFlipCode.Run(qsim, nbShots).Result;
                    Console.WriteLine("Result = {0}", result);
                    return 0;
                }
                else
                {
                    var result = DoBellFaultyMeasurementsBitFlipConcatenateCode.Run(qsim, nbShots).Result;
                    Console.WriteLine("Result = {0}", result);
                    return 0;
                }
            }
        }
    }
}
