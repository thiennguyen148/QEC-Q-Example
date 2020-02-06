// Copyright Thien Nguyen
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace QEC_Sample
{
    // (Classical) data-structure to keep track of the surface code qubit lattice.
    // We assume that data qubits (as indexed here) can be arbitrarily assigned to
    // underlying qubits. Otherwise, addition mapping needs to be implemented
    // in order to match the 2D layout of qubit array. 
    public class SurfaceCodeDatastructure
    {
        public class DataQubit
        {
            readonly Int64 m_id;
            public Int64 ID { get { return m_id; } }

            public DataQubit(Int64 id)
            {
                m_id = id;
            }
        }

        public class SyndromeQubit
        {
            public enum Type
            {
                X,
                Z
            }

            readonly Int64 m_id;
            readonly Type m_type;
            // A syndrome qubit is related to (up to) 4 data qubits.
            List<DataQubit> m_dataQubits;

            // Enable flag
            // e.g. we can disable a Syndrom qubit (i.e. not measuring that syndrome)
            // to create defects (holes) and move defects around (braiding)
            public bool Enable { get; set; }
            public Int64 ID { get { return m_id; } }
            public Type PauliType { get { return m_type; } }
            // Get the list of data qubit ID's whose X or Z syndrome is measured by this qubit.
            public IEnumerable<Int64> DataQubitIdx
            {
                get
                {
                    return m_dataQubits.Select(dataQ => dataQ.ID);
                }
            }

            public SyndromeQubit(Int64 id, Type type)
            {
                m_id = id;
                m_type = type;
                Enable = true;
                m_dataQubits = new List<DataQubit>();
            }

            public void AddDataQubit(DataQubit qubit)
            {
                m_dataQubits.Add(qubit);
            }

            public void PrintOut()
            {
                // For debug:
                Console.Write("Id: {0} Type: {1} Qubits: ", m_id, m_type);
                foreach (var qId in m_dataQubits)
                {
                    Console.Write("{0}, ", qId.ID);
                }

                Console.Write("\n");
            }

        }

        // Constructor: by width and height of the surface code lattice
        // Notes:
        // (1) the width and height here are the counts of *data* qubits along the edges of the lattice.
        // i.e. there are alternating rows/columns between these primary rows/columns as well.
        // (2) We are using the most basic (rectangular) layout, e.g. no lattice surgery/custom layout.
        public SurfaceCodeDatastructure(int width, int height)
        {
            m_dataQubits = new List<List<DataQubit>>();
            m_syndromeQubits = new List<SyndromeQubit>();
            m_syndromeMeasurements = new List<int>();
            m_lastSyndromeMeasurements = new List<int>();

            // Assumption: qubit is numbered row-by-row
            var qubitId = 0;
            for (int i = 0; i < height; i++)
            {
                var row = new List<DataQubit>();

                for (int j = 0; j < width; j++)
                {
                    var dataQubit = new DataQubit(qubitId);
                    qubitId++;
                    row.Add(dataQubit);
                }

                m_dataQubits.Add(row);

                if (i != height - 1)
                {
                    var altRow = new List<DataQubit>();
                    for (int jj = 0; jj < width - 1; jj++)
                    {
                        var dataQubit = new DataQubit(qubitId);
                        qubitId++;
                        altRow.Add(dataQubit);
                    }
                    m_dataQubits.Add(altRow);
                }
            }

            m_nbQubits = qubitId;

            var currentSyndromeType = SyndromeQubit.Type.X;
            for (int i = 0; i < 2 * height - 1; i++)
            {
                // Not the first and last row 
                if (i != 0 && i != 2 * height - 2)
                {
                    var dataQubitsRow = m_dataQubits[i];
                    var dataQubitsRowBefore = m_dataQubits[i - 1];
                    var dataQubitsRowAfter = m_dataQubits[i + 1];
                    Debug.Assert(dataQubitsRowBefore.Count == dataQubitsRowAfter.Count);

                    var expectedLength = dataQubitsRowBefore.Count;
                    var isAlt = expectedLength < dataQubitsRow.Count;
                    for (int ii = 0; ii < expectedLength; ++ii)
                    {
                        var syndromeQubit = new SyndromeQubit(qubitId, currentSyndromeType);
                        qubitId++;
                        if (isAlt)
                        {
                            syndromeQubit.AddDataQubit(dataQubitsRow[ii]);
                            syndromeQubit.AddDataQubit(dataQubitsRow[ii + 1]);
                            syndromeQubit.AddDataQubit(dataQubitsRowBefore[ii]);
                            syndromeQubit.AddDataQubit(dataQubitsRowAfter[ii]);
                        }
                        else
                        {
                            if (ii == 0)
                            {
                                syndromeQubit.AddDataQubit(dataQubitsRow[ii]);
                                syndromeQubit.AddDataQubit(dataQubitsRowBefore[ii]);
                                syndromeQubit.AddDataQubit(dataQubitsRowAfter[ii]);
                            }
                            else if (ii == expectedLength - 1)
                            {
                                syndromeQubit.AddDataQubit(dataQubitsRow[ii - 1]);
                                syndromeQubit.AddDataQubit(dataQubitsRowBefore[ii]);
                                syndromeQubit.AddDataQubit(dataQubitsRowAfter[ii]);
                            }
                            else
                            {
                                syndromeQubit.AddDataQubit(dataQubitsRow[ii - 1]);
                                syndromeQubit.AddDataQubit(dataQubitsRow[ii]);
                                syndromeQubit.AddDataQubit(dataQubitsRowBefore[ii]);
                                syndromeQubit.AddDataQubit(dataQubitsRowAfter[ii]);
                            }
                        }

                        m_syndromeQubits.Add(syndromeQubit);
                    }
                }
                else if (i == 0)
                {
                    // First row: must be alt
                    var dataQubitsRow = m_dataQubits[i];
                    var dataQubitsRowAfter = m_dataQubits[i + 1];
                    Debug.Assert(dataQubitsRowAfter.Count < dataQubitsRow.Count);

                    var expectedLength = dataQubitsRowAfter.Count;
                    for (int ii = 0; ii < expectedLength; ++ii)
                    {
                        var syndromeQubit = new SyndromeQubit(qubitId, currentSyndromeType);
                        qubitId++;
                        syndromeQubit.AddDataQubit(dataQubitsRow[ii]);
                        syndromeQubit.AddDataQubit(dataQubitsRow[ii + 1]);
                        syndromeQubit.AddDataQubit(dataQubitsRowAfter[ii]);
                        m_syndromeQubits.Add(syndromeQubit);
                    }
                }
                else
                {
                    // Last row: must be alt
                    var dataQubitsRow = m_dataQubits[i];
                    var dataQubitsRowBefore = m_dataQubits[i - 1];
                    Debug.Assert(dataQubitsRowBefore.Count < dataQubitsRow.Count);
                    var expectedLength = dataQubitsRowBefore.Count;
                    for (int ii = 0; ii < expectedLength; ++ii)
                    {
                        var syndromeQubit = new SyndromeQubit(qubitId, currentSyndromeType);
                        qubitId++;
                        syndromeQubit.AddDataQubit(dataQubitsRow[ii]);
                        syndromeQubit.AddDataQubit(dataQubitsRow[ii + 1]);
                        syndromeQubit.AddDataQubit(dataQubitsRowBefore[ii]);
                        m_syndromeQubits.Add(syndromeQubit);
                    }
                }
                // Toggle the syndrome type
                currentSyndromeType = (currentSyndromeType == SyndromeQubit.Type.Z) ? SyndromeQubit.Type.X : SyndromeQubit.Type.Z;
            }

            // For debug: print out the syndrome mappings
            //foreach (var syndrome in m_syndromeQubits)
            //{
            //    syndrome.PrintOut();
            //}

        }

        // Get the number of data qubits required for this surface code array
        public int NQubits { get { return m_nbQubits; } }

        public SyndromeQubit GetSyndromeQubit(int index)
        {
            return m_syndromeQubits[index];
        }

        // Note: this data structure keeps track data from all syndrome measurement rounds.
        // e.g. we can use that for error decoding.
        // Currently, no error decoding is implemented, just print out when it
        // detects any changes b/w syndome measurement rounds.
        public void AddMeasurementResult(int result)
        {
            m_syndromeMeasurements.Add(result);
            if (m_syndromeMeasurements.Count % NSyndromes == 0)
            {
                var lastSyndromeMeasurements = new List<int>();

                for (int i = m_syndromeMeasurements.Count - NSyndromes; i < m_syndromeMeasurements.Count; i++)
                {
                    lastSyndromeMeasurements.Add(m_syndromeMeasurements[i]);
                }

                if (m_lastSyndromeMeasurements.Count > 0)
                {
                    bool equal = true;
                    for (var i = 0; i < m_lastSyndromeMeasurements.Count; i++)
                    {
                        if (m_lastSyndromeMeasurements[i] != lastSyndromeMeasurements[i])
                        {
                            equal = false;
                            break;
                        }
                    }
                    if (!equal)
                    {
                        // Error detected
                        Console.Write("Parity changes detected:\n");
                        Console.Write("======= BEFORE ========\n");
                        PrintOut(m_lastSyndromeMeasurements.ToArray());
                        Console.Write("======= AFTER ========\n");
                        PrintOut(lastSyndromeMeasurements.ToArray());
                    }

                }
                // Update the cache
                m_lastSyndromeMeasurements.Clear();
                m_lastSyndromeMeasurements.AddRange(lastSyndromeMeasurements);
            }

        }

        // Pretty console printer: (looks like a 2D array)
        private void PrintOut(int[] syndromeData)
        {
            var isAlt = false;
            int syndromeCount = 0;
            int rowCount = 0;
            foreach (var row in m_dataQubits)
            {
                int syndromeCountPerRow = 0;
                foreach (var dataQ in row)
                {
                    if (!isAlt)
                    {
                        Console.Write("Q{0}", dataQ.ID.ToString("D2"));
                        if (syndromeCountPerRow < row.Count - 1)
                        {
                            Console.Write("--({0})--", syndromeData[syndromeCount] == 0 ? '+' : '-');
                            syndromeCount++;
                            syndromeCountPerRow++;
                        }
                    }
                    else
                    {
                        Console.Write("({0})--Q{1}--", syndromeData[syndromeCount] == 0 ? '+' : '-', dataQ.ID.ToString("D2"));
                        syndromeCount++;
                        syndromeCountPerRow++;
                        if (syndromeCountPerRow == row.Count)
                        {
                            Console.Write("({0})", syndromeData[syndromeCount] == 0 ? '+' : '-');
                            syndromeCount++;
                            syndromeCountPerRow++;
                        }
                    }
                }

                Console.Write("\n");
                rowCount++;
                if (rowCount < m_dataQubits.Count)
                {
                    int countPerRow = 0;
                    foreach (var dataQ in row)
                    {
                        if (!isAlt)
                        {
                            Console.Write(" | ");
                            if (countPerRow < row.Count - 1)
                            {
                                Console.Write("   |   ");
                                countPerRow++;
                            }
                        }
                        else
                        {
                            Console.Write(" |    |   ");
                            countPerRow++;
                            if (countPerRow == row.Count)
                            {
                                Console.Write(" | ");
                            }
                        }
                    }
                }


                isAlt = !isAlt;
                Console.Write("\n");

            }
        }

        public int NSyndromes { get { return m_syndromeQubits.Count; } }

        List<List<DataQubit>> m_dataQubits;
        int m_nbQubits;
        List<SyndromeQubit> m_syndromeQubits;
        List<int> m_syndromeMeasurements;
        List<int> m_lastSyndromeMeasurements;
    }
}
