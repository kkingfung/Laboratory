using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Advanced Mathematical Physics Engine for genetic algorithms.
    /// Applies concepts from statistical mechanics, quantum field theory,
    /// and non-linear dynamics to evolutionary computation.
    /// </summary>
    public static class AdvancedMathematicalPhysics
    {
        // Fundamental constants from physics
        private const float PLANCK_CONSTANT = 6.62607015e-34f; // J·s
        private const float BOLTZMANN_CONSTANT = 1.38064852e-23f; // J/K
        private const float HBAR = 1.054571817e-34f; // ℏ = h/2π
        private const float FINE_STRUCTURE_CONSTANT = 7.2973525693e-3f; // α ≈ 1/137

        /// <summary>
        /// Applies statistical mechanics to population dynamics using partition functions
        /// </summary>
        public static PopulationThermodynamics CalculatePopulationThermodynamics(
            List<float> fitnessValues,
            float temperature = 310f,
            float chemicalPotential = 0f)
        {
            var thermodynamics = new PopulationThermodynamics
            {
                Temperature = temperature,
                PartitionFunction = 0f,
                FreeEnergy = 0f,
                Entropy = 0f,
                AverageEnergy = 0f,
                HeatCapacity = 0f
            };

            if (fitnessValues.Count == 0) return thermodynamics;

            // Calculate partition function: Z = Σ e^(-E_i/kT)
            var kT = BOLTZMANN_CONSTANT * temperature;
            var partitionFunction = 0f;
            var energySum = 0f;
            var energySquaredSum = 0f;

            foreach (var fitness in fitnessValues)
            {
                // Convert fitness to energy (higher fitness = lower energy)
                var energy = -fitness * kT * 10f; // Scale factor for biological relevance
                var boltzmannFactor = Mathf.Exp(-(energy - chemicalPotential) / kT);

                partitionFunction += boltzmannFactor;
                energySum += energy * boltzmannFactor;
                energySquaredSum += energy * energy * boltzmannFactor;
            }

            thermodynamics.PartitionFunction = partitionFunction;

            if (partitionFunction > 0)
            {
                // Average energy: <E> = (1/Z) Σ E_i e^(-E_i/kT)
                thermodynamics.AverageEnergy = energySum / partitionFunction;

                // Free energy: F = -kT ln(Z)
                thermodynamics.FreeEnergy = -kT * Mathf.Log(partitionFunction);

                // Entropy: S = k ln(Z) + <E>/T
                thermodynamics.Entropy = BOLTZMANN_CONSTANT * Mathf.Log(partitionFunction) +
                                       thermodynamics.AverageEnergy / temperature;

                // Heat capacity: C = (<E²> - <E>²)/kT²
                var averageEnergySquared = energySquaredSum / partitionFunction;
                var energyVariance = averageEnergySquared - thermodynamics.AverageEnergy * thermodynamics.AverageEnergy;
                thermodynamics.HeatCapacity = energyVariance / (kT * kT);
            }

            return thermodynamics;
        }

        /// <summary>
        /// Applies quantum field theory concepts to genetic evolution
        /// </summary>
        public static QuantumFieldEvolution SimulateQuantumFieldEvolution(
            List<float> geneticField,
            float fieldCoupling = 0.1f,
            float massParameter = 1f)
        {
            var evolution = new QuantumFieldEvolution
            {
                FieldEnergy = 0f,
                VacuumExpectationValue = 0f,
                QuantumCorrections = new List<float>(),
                PhaseTransitions = new List<PhaseTransition>()
            };

            if (geneticField.Count == 0) return evolution;

            // Calculate field energy using Klein-Gordon equation
            var totalEnergy = 0f;
            var fieldSum = 0f;
            var fieldSquaredSum = 0f;

            for (int i = 0; i < geneticField.Count; i++)
            {
                var fieldValue = geneticField[i];
                fieldSum += fieldValue;
                fieldSquaredSum += fieldValue * fieldValue;

                // Kinetic energy term (simplified spatial derivative)
                var kineticEnergy = 0f;
                if (i > 0 && i < geneticField.Count - 1)
                {
                    var gradient = (geneticField[i + 1] - geneticField[i - 1]) / 2f;
                    kineticEnergy = 0.5f * gradient * gradient;
                }

                // Potential energy: V(φ) = ½m²φ² + λφ⁴/4!
                var potentialEnergy = 0.5f * massParameter * massParameter * fieldValue * fieldValue +
                                    fieldCoupling * fieldValue * fieldValue * fieldValue * fieldValue / 24f;

                totalEnergy += kineticEnergy + potentialEnergy;
            }

            evolution.FieldEnergy = totalEnergy;

            // Vacuum expectation value (spontaneous symmetry breaking)
            evolution.VacuumExpectationValue = fieldSum / geneticField.Count;

            // One-loop quantum corrections (simplified)
            for (int i = 0; i < geneticField.Count; i++)
            {
                var classicalValue = geneticField[i];

                // Quantum correction: δφ = ℏλ/(16π²m²) ln(Λ²/m²)
                var quantumCorrection = HBAR * fieldCoupling / (16f * Mathf.PI * Mathf.PI * massParameter * massParameter) *
                                      Mathf.Log(100f / (massParameter * massParameter)); // Λ = 10 (cutoff)

                evolution.QuantumCorrections.Add(quantumCorrection);

                // Check for phase transitions (critical points)
                if (i > 0)
                {
                    var derivative = geneticField[i] - geneticField[i - 1];
                    if (Mathf.Abs(derivative) > 0.5f) // Critical derivative threshold
                    {
                        evolution.PhaseTransitions.Add(new PhaseTransition
                        {
                            Position = i,
                            OrderParameter = classicalValue,
                            CriticalExponent = CalculateCriticalExponent(derivative)
                        });
                    }
                }
            }

            return evolution;
        }

        /// <summary>
        /// Applies non-linear dynamics and chaos theory to mutation patterns
        /// </summary>
        public static ChaoticMutationAnalysis AnalyzeChaoticMutations(
            List<float> mutationRates,
            float controlParameter = 3.5f)
        {
            var analysis = new ChaoticMutationAnalysis
            {
                LyapunovExponent = 0f,
                AttractorDimension = 0f,
                BifurcationPoints = new List<float>(),
                ChaoticRegions = new List<ChaoticRegion>()
            };

            if (mutationRates.Count < 3) return analysis;

            // Calculate Lyapunov exponent for chaos detection
            analysis.LyapunovExponent = CalculateLyapunovExponent(mutationRates);

            // Calculate correlation dimension (attractor dimension)
            analysis.AttractorDimension = CalculateCorrelationDimension(mutationRates);

            // Analyze using logistic map: x_{n+1} = r*x_n*(1-x_n)
            var logisticSeries = new List<float>();
            var x = 0.5f; // Initial condition

            for (int i = 0; i < 1000; i++) // Generate long series
            {
                x = controlParameter * x * (1f - x);
                logisticSeries.Add(x);
            }

            // Find bifurcation points by varying control parameter
            for (float r = 1f; r <= 4f; r += 0.01f)
            {
                var maxima = FindLocalMaxima(GenerateLogisticSeries(r, 1000));
                if (maxima.Count != FindLocalMaxima(GenerateLogisticSeries(r - 0.01f, 1000)).Count)
                {
                    analysis.BifurcationPoints.Add(r);
                }
            }

            // Identify chaotic regions (Lyapunov > 0)
            analysis.ChaoticRegions = IdentifyChaoticRegions(mutationRates);

            return analysis;
        }

        /// <summary>
        /// Applies renormalization group theory to scale-invariant genetic patterns
        /// </summary>
        public static RenormalizationGroupAnalysis ApplyRenormalizationGroup(
            List<float> geneticData,
            int scaleFactors = 3)
        {
            var analysis = new RenormalizationGroupAnalysis
            {
                BetaFunction = 0f,
                FixedPoints = new List<float>(),
                CriticalExponents = new List<float>(),
                ScalingDimensions = new List<float>()
            };

            if (geneticData.Count < 4) return analysis;

            // Coarse-graining procedure: average over blocks
            var currentData = new List<float>(geneticData);

            for (int scale = 0; scale < scaleFactors; scale++)
            {
                var coarseGrainedData = new List<float>();

                // Block averaging with factor of 2
                for (int i = 0; i < currentData.Count - 1; i += 2)
                {
                    var average = (currentData[i] + currentData[i + 1]) / 2f;
                    coarseGrainedData.Add(average);
                }

                if (coarseGrainedData.Count == 0) break;

                // Calculate beta function: β(g) = dg/d ln(L)
                if (scale > 0)
                {
                    var coupling = CalculateEffectiveCoupling(coarseGrainedData);
                    var previousCoupling = CalculateEffectiveCoupling(currentData);

                    var betaFunction = (coupling - previousCoupling) / Mathf.Log(2f); // Scale factor = 2
                    analysis.BetaFunction = betaFunction;

                    // Fixed points: β(g*) = 0
                    if (Mathf.Abs(betaFunction) < 0.01f)
                    {
                        analysis.FixedPoints.Add(coupling);
                    }
                }

                // Calculate scaling dimension
                var scalingDimension = CalculateScalingDimension(currentData, coarseGrainedData);
                analysis.ScalingDimensions.Add(scalingDimension);

                // Calculate critical exponent
                var criticalExponent = CalculateCriticalExponentFromScaling(scalingDimension);
                analysis.CriticalExponents.Add(criticalExponent);

                currentData = coarseGrainedData;
            }

            return analysis;
        }

        /// <summary>
        /// Applies gauge theory to evolutionary symmetries
        /// </summary>
        public static GaugeTheoryEvolution AnalyzeEvolutionaryGaugeSymmetry(
            List<Vector3> evolutionaryStates,
            float gaugeCoupling = 0.3f)
        {
            var analysis = new GaugeTheoryEvolution
            {
                WilsonLoop = 0f,
                TopologicalCharge = 0,
                ActionDensity = 0f,
                SymmetryBreaking = false
            };

            if (evolutionaryStates.Count < 4) return analysis;

            // Calculate Wilson loop for closed evolutionary paths
            analysis.WilsonLoop = CalculateWilsonLoop(evolutionaryStates, gaugeCoupling);

            // Calculate topological charge (winding number)
            analysis.TopologicalCharge = CalculateTopologicalCharge(evolutionaryStates);

            // Calculate Yang-Mills action density
            analysis.ActionDensity = CalculateYangMillsAction(evolutionaryStates, gaugeCoupling);

            // Check for spontaneous symmetry breaking
            analysis.SymmetryBreaking = CheckSymmetryBreaking(evolutionaryStates);

            return analysis;
        }

        // Helper methods for mathematical physics calculations

        private static float CalculateLyapunovExponent(List<float> data)
        {
            if (data.Count < 3) return 0f;

            var sum = 0f;
            var count = 0;

            for (int i = 1; i < data.Count - 1; i++)
            {
                var derivative = Mathf.Abs((data[i + 1] - data[i - 1]) / 2f);
                if (derivative > 0)
                {
                    sum += Mathf.Log(derivative);
                    count++;
                }
            }

            return count > 0 ? sum / count : 0f;
        }

        private static float CalculateCorrelationDimension(List<float> data)
        {
            // Simplified correlation dimension calculation
            var distances = new List<float>();

            for (int i = 0; i < data.Count; i++)
            {
                for (int j = i + 1; j < data.Count; j++)
                {
                    distances.Add(Mathf.Abs(data[i] - data[j]));
                }
            }

            distances.Sort();

            var correlationSum = 0f;
            var epsilon = 0.1f;

            foreach (var distance in distances)
            {
                if (distance < epsilon)
                {
                    correlationSum += 1f;
                }
            }

            var totalPairs = distances.Count;
            var correlationDimension = totalPairs > 0 ?
                Mathf.Log(correlationSum / totalPairs) / Mathf.Log(epsilon) : 0f;

            return Mathf.Max(0f, correlationDimension);
        }

        private static List<float> GenerateLogisticSeries(float r, int length)
        {
            var series = new List<float>();
            var x = 0.5f;

            for (int i = 0; i < length; i++)
            {
                x = r * x * (1f - x);
                series.Add(x);
            }

            return series;
        }

        private static List<float> FindLocalMaxima(List<float> data)
        {
            var maxima = new List<float>();

            for (int i = 1; i < data.Count - 1; i++)
            {
                if (data[i] > data[i - 1] && data[i] > data[i + 1])
                {
                    maxima.Add(data[i]);
                }
            }

            return maxima;
        }

        private static List<ChaoticRegion> IdentifyChaoticRegions(List<float> data)
        {
            var regions = new List<ChaoticRegion>();
            var windowSize = 10;

            for (int i = 0; i <= data.Count - windowSize; i++)
            {
                var window = data.Skip(i).Take(windowSize).ToList();
                var lyapunov = CalculateLyapunovExponent(window);

                if (lyapunov > 0)
                {
                    regions.Add(new ChaoticRegion
                    {
                        StartIndex = i,
                        EndIndex = i + windowSize - 1,
                        LyapunovExponent = lyapunov
                    });
                }
            }

            return regions;
        }

        private static float CalculateEffectiveCoupling(List<float> data)
        {
            if (data.Count < 2) return 0f;

            var variance = 0f;
            var mean = data.Average();

            foreach (var value in data)
            {
                variance += (value - mean) * (value - mean);
            }

            variance /= data.Count - 1;
            return variance; // Simplified effective coupling
        }

        private static float CalculateScalingDimension(List<float> original, List<float> coarseGrained)
        {
            if (original.Count == 0 || coarseGrained.Count == 0) return 0f;

            var originalVariance = CalculateVariance(original);
            var coarseGrainedVariance = CalculateVariance(coarseGrained);

            if (originalVariance <= 0 || coarseGrainedVariance <= 0) return 0f;

            return Mathf.Log(coarseGrainedVariance / originalVariance) / Mathf.Log(2f);
        }

        private static float CalculateVariance(List<float> data)
        {
            if (data.Count == 0) return 0f;

            var mean = data.Average();
            var variance = data.Sum(x => (x - mean) * (x - mean)) / data.Count;
            return variance;
        }

        private static float CalculateCriticalExponentFromScaling(float scalingDimension)
        {
            // Simplified critical exponent calculation
            return 2f - scalingDimension;
        }

        private static float CalculateCriticalExponent(float derivative)
        {
            return Mathf.Abs(derivative) > 0 ? 1f / Mathf.Log(Mathf.Abs(derivative)) : 0f;
        }

        private static float CalculateWilsonLoop(List<Vector3> states, float coupling)
        {
            if (states.Count < 3) return 0f;

            var wilsonLoop = 1f;

            for (int i = 0; i < states.Count; i++)
            {
                var current = states[i];
                var next = states[(i + 1) % states.Count];

                // Simplified gauge connection
                var connection = Vector3.Dot(current, next) * coupling;
                wilsonLoop *= Mathf.Exp(connection);
            }

            return wilsonLoop;
        }

        private static int CalculateTopologicalCharge(List<Vector3> states)
        {
            if (states.Count < 3) return 0;

            var windingNumber = 0f;

            for (int i = 0; i < states.Count; i++)
            {
                var current = states[i];
                var next = states[(i + 1) % states.Count];

                var angle = Vector3.SignedAngle(current, next, Vector3.up);
                windingNumber += angle;
            }

            return Mathf.RoundToInt(windingNumber / 360f);
        }

        private static float CalculateYangMillsAction(List<Vector3> states, float coupling)
        {
            if (states.Count < 2) return 0f;

            var action = 0f;

            for (int i = 0; i < states.Count - 1; i++)
            {
                var fieldStrength = (states[i + 1] - states[i]).magnitude;
                action += fieldStrength * fieldStrength / (4f * coupling * coupling);
            }

            return action;
        }

        private static bool CheckSymmetryBreaking(List<Vector3> states)
        {
            if (states.Count == 0) return false;

            var center = Vector3.zero;
            foreach (var state in states)
            {
                center += state;
            }
            center /= states.Count;

            // Check if the average deviates significantly from origin
            return center.magnitude > 0.1f;
        }
    }

    // Supporting data structures for advanced mathematical physics
    public struct PopulationThermodynamics
    {
        public float Temperature;
        public float PartitionFunction;
        public float FreeEnergy;
        public float Entropy;
        public float AverageEnergy;
        public float HeatCapacity;
    }

    public struct QuantumFieldEvolution
    {
        public float FieldEnergy;
        public float VacuumExpectationValue;
        public List<float> QuantumCorrections;
        public List<PhaseTransition> PhaseTransitions;
    }

    public struct PhaseTransition
    {
        public int Position;
        public float OrderParameter;
        public float CriticalExponent;
    }

    public struct ChaoticMutationAnalysis
    {
        public float LyapunovExponent;
        public float AttractorDimension;
        public List<float> BifurcationPoints;
        public List<ChaoticRegion> ChaoticRegions;
    }

    public struct ChaoticRegion
    {
        public int StartIndex;
        public int EndIndex;
        public float LyapunovExponent;
    }

    public struct RenormalizationGroupAnalysis
    {
        public float BetaFunction;
        public List<float> FixedPoints;
        public List<float> CriticalExponents;
        public List<float> ScalingDimensions;
    }

    public struct GaugeTheoryEvolution
    {
        public float WilsonLoop;
        public int TopologicalCharge;
        public float ActionDensity;
        public bool SymmetryBreaking;
    }
}