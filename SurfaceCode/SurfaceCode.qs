// Copyright Thien Nguyen
// Licensed under the MIT License.

namespace Microsoft.Quantum.Samples.SurfaceCode {
    open Microsoft.Quantum.ErrorCorrection;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    
    // This is a simple example demonstrating surface code operation.
    // The example is constructed as a closely-coupled quantum device - classical host pair,
    // in which high-level classical logic (syndrome measurements, including opening/closing holes, applying gates to qubits)
    // is implemented by the host (as C#). The Q# operation hence becomes an infinite loop of error correction cycles (and occasionally applying gates).
    // We use the following convention for Pauli operators (as Int64): X(1), Y(2), Z(3)
    // For stablizer group: the data array (from host/controller) is [Pauli as Int, list of qubit indices]
    // For additional operators to apply at the end of a cycle has the format: [Pauli type, Index, Pauli type, Index, ...] 
    // Note: for the sake of simulation, we only have a *single* syndrome qubit, i.e. syndrome measurements are implemented sequentially.
    // This is obviously not the way surface code will be run, however, if time-scale is irrelevant (like in the simulation environment here),
    // it doesn't matter.
    operation RunSurfaceCodeErrorCorrectionCycle(nbQubits: Int, stabilizerGroupGen: (Unit -> Int[]), reportStabilizerResult: (Int -> Unit), applyOpGetFn: (Unit -> Int[])) : Unit {
        using ((dataQubits, ancilla) = (Qubit[nbQubits], Qubit())) {
            mutable hasStabilizerToMeasure = 1;
            repeat {
                let nextStabilizerGroupToMeasure = stabilizerGroupGen();
                if (Length(nextStabilizerGroupToMeasure) == 0) {
                    set hasStabilizerToMeasure = 0;
                }
                else {
                    if (nextStabilizerGroupToMeasure[0] == 3) {
                        // Pauli Z
                        Reset(ancilla);
                        for (index in 1 .. Length(nextStabilizerGroupToMeasure) - 1) {
                            CNOT(dataQubits[nextStabilizerGroupToMeasure[index]], ancilla);
                        }
                        let syndromeResult = M(ancilla);
                        Reset(ancilla);
                        if (syndromeResult == One) {
                            reportStabilizerResult(1); 
                        }
                        else {
                            reportStabilizerResult(0); 
                        }
                    }
                    elif (nextStabilizerGroupToMeasure[0] == 1){
                        Reset(ancilla);
                        H(ancilla);
                        for (index in 1 .. Length(nextStabilizerGroupToMeasure) - 1) {
                            CNOT(ancilla, dataQubits[nextStabilizerGroupToMeasure[index]]);
                        }
                        H(ancilla);
                        let syndromeResult = M(ancilla);
                        Reset(ancilla);
                        if (syndromeResult == One) {
                            reportStabilizerResult(1); 
                        }
                        else {
                            reportStabilizerResult(0); 
                        }
                    }
                    else {
                        set hasStabilizerToMeasure = 0;
                    }
                    
                    
                    let qubitToApplyError = applyOpGetFn();

                    for (index in 0..2..Length(qubitToApplyError) - 1) {

                        let opCode = qubitToApplyError[index];
                        let qIdx = qubitToApplyError[index + 1];
                        if (opCode == 1) {
                            Message($"Apply X at q{qIdx}.");
                            X(dataQubits[qIdx]);
                        }
                        elif (opCode == 2) {
                            Message($"Apply Y at q{qIdx}.");
                            Y(dataQubits[qIdx]);
                        }
                        else {
                            Message($"Apply Z at q{qIdx}.");
                            Z(dataQubits[qIdx]);
                        }
                    }
                }
                
            }
            until (hasStabilizerToMeasure == 0);
            Message($"Finish! Reset all qubits.");
            ResetAll(dataQubits);
        }
    }

    
}
