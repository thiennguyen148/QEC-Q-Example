// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Samples.SimulatorWithOverrides {
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;

    operation EncodeIntoBitFlipCode (data : Qubit, auxiliaryQubits : Qubit[]) : Unit {
        CNOT(data, auxiliaryQubits[0]);
        CNOT(data, auxiliaryQubits[0]);
    }

    operation MeasureLogicalQubit(register : Qubit[]) : Result {
        let meas0 = M(register[0]);
        let meas1 = M(register[1]);
        if (meas0 == meas1) {
            return meas0;
        }
        else {
            let meas2 = M(register[2]);
            // meas2 is the tie-breaker, we believe whatever it said :)
            return meas2;
        }
    }

    operation RunErrorCorrectionCycle (logicalQubit : Qubit[]) : Unit {
        using (ancilla = Qubit()) {
            CNOT(logicalQubit[0], ancilla);
            CNOT(logicalQubit[1], ancilla);
            let parity01 = M(ancilla);
            ResetAll([ancilla]);
            CNOT(logicalQubit[1], ancilla);
            CNOT(logicalQubit[2], ancilla);
            let parity12 = M(ancilla);
            ResetAll([ancilla]);
            
            // Correct error
            if (parity01 == One and parity12 == Zero) {
                X(logicalQubit[0]);
            }
            elif (parity01 == One and parity12 == One) {
                X(logicalQubit[1]);
            }
            elif (parity01 == Zero and parity12 == One) {
                X(logicalQubit[2]);
            }
        }
   }
    /// # Summary
    /// Run a series of experiments that on a perfect state simulator produce matching measurement results.
    /// If executed on a simulator which introduces errors in measurements, 
    /// a certain percentage of experiments will produce mismatched results.
    operation DoFaultyMeasurements (nbRuns: Int) : Int {
        let nRuns = nbRuns;
        mutable nCorrect = 0;
        for (i in 1 .. nRuns) {
            using (q1 = Qubit()) {
                // Measure qubit, if no error, should get a zero
                if (M(q1) == Zero) {
                    set nCorrect += 1;
                }

                // Make sure to return the qubits to 0 state
                ResetAll([q1]);
            }
        }
        Message($"Single Qubit: {nCorrect} runs out of {nRuns} produced the correct results.");
        return nCorrect;
    }

    operation DoFaultyMeasurementsBitFlipCode (nbRuns: Int) : Int {
        let nRuns = nbRuns;
        mutable nCorrect = 0;
        for (i in 1 .. nRuns) {
            using ((data, auxiliaryQubits) = (Qubit(), Qubit[2])) {
                let register = [data] + auxiliaryQubits;

                // Next, we encode our state (by using CNOT gates).
                CNOT(data, auxiliaryQubits[0]);
                CNOT(data, auxiliaryQubits[1]);

                // Do measurements
                let meas0 = M(register[0]);
                let meas1 = M(register[1]);
                if (meas0 == meas1) {
                    if (meas0 == Zero) {
                        set nCorrect += 1;
                    }
                }
                else {
                    let meas2 = M(register[2]);
                    // meas2 is the tie-breaker, we believe whatever it said :)
                    if (meas2 == Zero) {
                        set nCorrect += 1;
                    }
                }
                ResetAll(register);
            }
        }
        Message($"FlipCode3: {nCorrect} runs out of {nRuns} produced the correct results.");
        return nCorrect;
    }

    operation DoBellFaultyMeasurements (nbRuns: Int) : Int {
        let nRuns = nbRuns;
        mutable nCorrect = 0;
        for (i in 1 .. nRuns) {
            using ((q1, q2) = (Qubit(), Qubit())) {
                // Prepare a Bell pair (in this state the measurement results on two qubits should be the same)
                H(q1);
                CNOT(q1, q2);
            
                // Measure both qubits; if there is an error introduced during one of the measurements (but not both), the results will diverge
                if (M(q1) == M(q2)) {
                    set nCorrect += 1;
                }

                // Make sure to return the qubits to 0 state
                ResetAll([q1, q2]);
            }
        }
        Message($"Single Qubit: {nCorrect} runs out of {nRuns} produced the correct results.");
        return nCorrect;
    }

    operation LogicalCNOT(reg1: Qubit[], reg2: Qubit[]): Unit {
        CNOT(reg1[0], reg2[0]);
        CNOT(reg1[1], reg2[1]);
        CNOT(reg1[1], reg2[2]);
    }

    operation DoBellFaultyMeasurementsBitFlipCode (nbRuns: Int) : Int {
        let nRuns = nbRuns;
        mutable nCorrect = 0;
        for (i in 1 .. nRuns) {
            using ((data1, auxiliaryQubits1, data2, auxiliaryQubits2) = (Qubit(), Qubit[2], Qubit(), Qubit[2]))  {
                let register1 = [data1] + auxiliaryQubits1;
                let register2 = [data2] + auxiliaryQubits2;
                
                H(data1);
                EncodeIntoBitFlipCode(data1, auxiliaryQubits1);
                EncodeIntoBitFlipCode(data2, auxiliaryQubits2);
                
                LogicalCNOT(register1, register2);
                
                RunErrorCorrectionCycle(register1);
                RunErrorCorrectionCycle(register2);

                // Do measurements
                if (MeasureLogicalQubit(register1) == MeasureLogicalQubit(register1)) {
                    set nCorrect += 1;
                }

                ResetAll(register1);
                ResetAll(register2);
            }
        }
        Message($"FlipCode3: {nCorrect} runs out of {nRuns} produced the correct results.");
        return nCorrect;
    }


    operation EncodeIntoBitFlipConcatenateCode(data: Qubit, auxiliaryQubits: Qubit[]) : Unit {
        let register1 = [data] + auxiliaryQubits[0..1];
        let register2 =  auxiliaryQubits[2..4];
        let register3 =  auxiliaryQubits[5..7];
        let groupAuxiliaryQubits = [register2[0]] + [register3[0]];
                
        // Next, we encode our test state.
        // Encode from data qubit to the *master* qubit of each group
        EncodeIntoBitFlipCode(data, groupAuxiliaryQubits);
        // Encode each group
        EncodeIntoBitFlipCode(register2[0], register2[1..2]);
        EncodeIntoBitFlipCode(register3[0], register3[1..2]);
    }

    operation MeasureConcatenateCode(qubitRegister: Qubit[]) : Result {
        let register1 = qubitRegister[0..2];
        let register2 =  qubitRegister[3..5];
        let register3 =  qubitRegister[6..8];
                
        // Check if we are in zero state:
        let meas0 = MeasureLogicalQubit(register1);
        let meas1 = MeasureLogicalQubit(register2);
        if (meas0 == meas1)
        {
            return meas0;
        }
        else
        {
            // Last *encoded* qubit is the tie-breaker, we believe whatever it said :)
            return MeasureLogicalQubit(register3);
        }
    }

    operation DoFaultyMeasurementsBitFlipConcatenateCode (nbRuns: Int) : Int {
        let nRuns = nbRuns;
        mutable nCorrect = 0;
        for (i in 1 .. nRuns) {
            using ((data, auxiliaryQubits) = (Qubit(), Qubit[8])) {
                EncodeIntoBitFlipConcatenateCode(data, auxiliaryQubits);
                let registerQ = [data] + auxiliaryQubits;
                
                // Check if we are in zero state:
                if (MeasureConcatenateCode(registerQ) == Zero)
                {
                    set nCorrect += 1;
                }
                                
                ResetAll(registerQ);
            }
        }
        Message($"FlipCodeConcatenated(3x3): {nCorrect} runs out of {nRuns} produced the correct results.");
        return nCorrect;
   }
    
    operation LogicalX (reg : Qubit[]): Unit {
        X(reg[0]);
        X(reg[1]);
        X(reg[2]);
    }    

    operation RunErrorCorrectionCycleConcatenate (logicalQubit : Qubit[]) : Unit {
        let register1 = logicalQubit[0..2];
        let register2 =  logicalQubit[3..5];
        let register3 =  logicalQubit[6..8];
        // Error correction for each group (3-qubit code)
        RunErrorCorrectionCycle(register1);
        RunErrorCorrectionCycle(register2);
        RunErrorCorrectionCycle(register3);

        // Group to group parity
        using (ancilla = Qubit[3]) {
            LogicalCNOT(register1, ancilla);
            LogicalCNOT(register2, ancilla);
            let parity1 = MeasureLogicalQubit(ancilla);
            ResetAll(ancilla);
            
            LogicalCNOT(register2, ancilla);
            LogicalCNOT(register3, ancilla);
            let parity2 = MeasureLogicalQubit(ancilla);
            ResetAll(ancilla);
            
            // Correct error
            if (parity1 == One and parity2 == Zero) {
                LogicalX(register1);
            }
            elif (parity1 == One and parity2 == One) {
                LogicalX(register2);
            }
            elif (parity1 == Zero and parity2 == One) {
                LogicalX(register3);
            }
        }
    }
    
    operation LogicalCNOTConcetenate (reg1 : Qubit[], reg2: Qubit[]) : Unit {
        let register11 = reg1[0..2];
        let register12 =  reg1[3..5];
        let register13 =  reg1[6..8];

        let register21 = reg2[0..2];
        let register22 =  reg2[3..5];
        let register23 =  reg2[6..8];
        
        LogicalCNOT(register11, register21);
        LogicalCNOT(register12, register22);
        LogicalCNOT(register13, register23);
    }

    operation DoBellFaultyMeasurementsBitFlipConcatenateCode (nbRuns: Int) : Int {
        let nRuns = nbRuns;
        mutable nCorrect = 0;
        for (i in 1 .. nRuns) {
            using ((data1, auxiliaryQubits1, data2, auxiliaryQubits2) = (Qubit(), Qubit[8], Qubit(), Qubit[8])) {
                H(data1);
                
                EncodeIntoBitFlipConcatenateCode(data1, auxiliaryQubits1);
                EncodeIntoBitFlipConcatenateCode(data2, auxiliaryQubits2);
                
                let register1 = [data1] + auxiliaryQubits1;
                let register2 = [data2] + auxiliaryQubits2;

                LogicalCNOTConcetenate(register1, register2);
                
                RunErrorCorrectionCycleConcatenate(register1);
                RunErrorCorrectionCycleConcatenate(register2);

                // Do measurements
                if (MeasureConcatenateCode(register1) == MeasureConcatenateCode(register1)) {
                    set nCorrect += 1;
                }
                
                ResetAll(register1);
                ResetAll(register2);
            }
        }
        Message($"FlipCodeConcatenated(3x3): {nCorrect} runs out of {nRuns} produced the correct results.");
        return nCorrect;
   }
}
