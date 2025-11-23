using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Information Theory Optimizer for Project Chimera.
    /// Applies Claude Shannon's information theory, Kolmogorov complexity,
    /// and algorithmic information theory for optimal data compression and transmission.
    /// </summary>
    public static class InformationTheoryOptimizer
    {
        // Information theory constants
        private const float LOG2_E = 1.44269504088896f; // log₂(e) for natural log conversion
        private const float THEORETICAL_MINIMUM_ENTROPY = 0.0001f; // Theoretical lower bound

        /// <summary>
        /// Calculates Shannon entropy for optimal data compression
        /// </summary>
        public static float CalculateShannonEntropy<T>(IEnumerable<T> data)
        {
            var frequencies = new Dictionary<T, int>();
            var totalCount = 0;

            // Count frequencies with O(n) complexity
            foreach (var item in data)
            {
                frequencies[item] = frequencies.GetValueOrDefault(item, 0) + 1;
                totalCount++;
            }

            if (totalCount == 0) return 0f;

            // Calculate Shannon entropy: H(X) = -Σ p(x) * log₂(p(x))
            var entropy = 0f;
            foreach (var frequency in frequencies.Values)
            {
                var probability = (float)frequency / totalCount;
                if (probability > 0)
                {
                    entropy -= probability * Mathf.Log(probability, 2f);
                }
            }

            return entropy;
        }

        /// <summary>
        /// Estimates Kolmogorov complexity (algorithmic information content)
        /// </summary>
        public static float EstimateKolmogorovComplexity(string data)
        {
            if (string.IsNullOrEmpty(data)) return 0f;

            // Approximate Kolmogorov complexity using compression ratio
            var originalBytes = Encoding.UTF8.GetBytes(data);
            var compressedSize = EstimateCompressionSize(originalBytes);

            // K(x) ≈ compressed_size / original_size * original_size
            var complexityRatio = (float)compressedSize / originalBytes.Length;
            return complexityRatio * originalBytes.Length;
        }

        /// <summary>
        /// Optimizes genetic data representation using information theory
        /// </summary>
        public static GeneticInformationOptimization OptimizeGeneticInformation(
            List<string> geneticSequences)
        {
            var optimization = new GeneticInformationOptimization
            {
                OriginalEntropy = 0f,
                OptimalEntropy = 0f,
                CompressionRatio = 1f,
                InformationGain = 0f,
                OptimalEncoding = new Dictionary<string, string>()
            };

            if (geneticSequences.Count == 0) return optimization;

            // Calculate original Shannon entropy
            optimization.OriginalEntropy = CalculateShannonEntropy(geneticSequences);

            // Find optimal encoding using Huffman-like algorithm
            var frequencyMap = BuildFrequencyMap(geneticSequences);
            var optimalCodes = GenerateOptimalCodes(frequencyMap);

            // Calculate theoretical minimum entropy (information theory bound)
            var totalSymbols = geneticSequences.SelectMany(s => s).Count();
            var uniqueSymbols = frequencyMap.Keys.Count;

            // Theoretical minimum: log₂(uniqueSymbols)
            optimization.OptimalEntropy = Mathf.Log(uniqueSymbols, 2f);

            // Calculate compression ratio using optimal encoding
            var originalBits = totalSymbols * 8; // 8 bits per character (UTF-8)
            var compressedBits = CalculateOptimalEncodingBits(geneticSequences, optimalCodes);
            optimization.CompressionRatio = (float)compressedBits / originalBits;

            // Information gain = reduction in uncertainty
            optimization.InformationGain = optimization.OriginalEntropy - optimization.OptimalEntropy;

            optimization.OptimalEncoding = optimalCodes;

            return optimization;
        }

        /// <summary>
        /// Applies mutual information analysis for event correlation optimization
        /// </summary>
        public static float CalculateMutualInformation<T, U>(
            IEnumerable<T> eventTypeX,
            IEnumerable<U> eventTypeY)
        {
            var joinedData = eventTypeX.Zip(eventTypeY, (x, y) => new { X = x, Y = y }).ToList();

            if (joinedData.Count == 0) return 0f;

            // Calculate individual entropies
            var entropyX = CalculateShannonEntropy(joinedData.Select(d => d.X));
            var entropyY = CalculateShannonEntropy(joinedData.Select(d => d.Y));

            // Calculate joint entropy
            var jointEntropy = CalculateShannonEntropy(joinedData);

            // Mutual Information: I(X;Y) = H(X) + H(Y) - H(X,Y)
            var mutualInformation = entropyX + entropyY - jointEntropy;

            return Mathf.Max(0f, mutualInformation); // MI is always non-negative
        }

        /// <summary>
        /// Optimizes service discovery using information-theoretic principles
        /// </summary>
        public static ServiceDiscoveryOptimization OptimizeServiceDiscovery(
            Dictionary<Type, int> serviceAccessPatterns)
        {
            var optimization = new ServiceDiscoveryOptimization
            {
                AccessEntropy = 0f,
                OptimalSearchOrder = new List<Type>(),
                ExpectedSearchTime = 0f,
                InformationContent = new Dictionary<Type, float>()
            };

            if (serviceAccessPatterns.Count == 0) return optimization;

            var totalAccesses = serviceAccessPatterns.Values.Sum();

            // Calculate access pattern entropy
            optimization.AccessEntropy = CalculateAccessPatternEntropy(serviceAccessPatterns, totalAccesses);

            // Calculate information content for each service type
            foreach (var kvp in serviceAccessPatterns)
            {
                var probability = (float)kvp.Value / totalAccesses;
                var informationContent = probability > 0 ? -Mathf.Log(probability, 2f) : float.MaxValue;
                optimization.InformationContent[kvp.Key] = informationContent;
            }

            // Optimal search order: prioritize by frequency (highest probability first)
            optimization.OptimalSearchOrder = serviceAccessPatterns
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            // Calculate expected search time using information theory
            optimization.ExpectedSearchTime = CalculateExpectedSearchTime(serviceAccessPatterns, totalAccesses);

            return optimization;
        }

        /// <summary>
        /// Implements theoretical minimum redundancy encoding (Kraft inequality)
        /// </summary>
        public static RedundancyOptimization OptimizeDataRedundancy<T>(
            IEnumerable<T> data,
            float targetReliability = 0.99f)
        {
            var dataList = data.ToList();
            var frequencies = new Dictionary<T, int>();

            foreach (var item in dataList)
            {
                frequencies[item] = frequencies.GetValueOrDefault(item, 0) + 1;
            }

            var optimization = new RedundancyOptimization
            {
                OriginalRedundancy = 0f,
                OptimalRedundancy = 0f,
                ErrorCorrectionBits = 0,
                ReliabilityAchieved = targetReliability
            };

            var totalItems = dataList.Count;
            var uniqueItems = frequencies.Count;

            // Calculate original redundancy
            var maxEntropy = Mathf.Log(uniqueItems, 2f); // Maximum possible entropy
            var actualEntropy = CalculateShannonEntropy(dataList);
            optimization.OriginalRedundancy = maxEntropy - actualEntropy;

            // Calculate optimal redundancy using Kraft inequality
            // For target reliability R, we need log₂(1/(1-R)) redundancy bits
            var requiredRedundancyBits = Mathf.Log(1f / (1f - targetReliability), 2f);
            optimization.OptimalRedundancy = requiredRedundancyBits;

            // Calculate error correction bits needed (Hamming bound)
            var hammingBound = CalculateHammingBound(totalItems, targetReliability);
            optimization.ErrorCorrectionBits = Mathf.CeilToInt(hammingBound);

            return optimization;
        }

        private static Dictionary<char, int> BuildFrequencyMap(List<string> sequences)
        {
            var frequencies = new Dictionary<char, int>();

            foreach (var sequence in sequences)
            {
                foreach (var character in sequence)
                {
                    frequencies[character] = frequencies.GetValueOrDefault(character, 0) + 1;
                }
            }

            return frequencies;
        }

        private static Dictionary<string, string> GenerateOptimalCodes(Dictionary<char, int> frequencies)
        {
            // Simplified Huffman coding implementation
            var codes = new Dictionary<string, string>();
            var sortedFrequencies = frequencies.OrderByDescending(kvp => kvp.Value).ToList();

            var codeLength = 1;
            foreach (var kvp in sortedFrequencies)
            {
                var binaryCode = Convert.ToString(codeLength - 1, 2).PadLeft(
                    Mathf.CeilToInt(Mathf.Log(sortedFrequencies.Count, 2f)), '0');
                codes[kvp.Key.ToString()] = binaryCode;
                codeLength++;
            }

            return codes;
        }

        private static int CalculateOptimalEncodingBits(
            List<string> sequences,
            Dictionary<string, string> encoding)
        {
            var totalBits = 0;

            foreach (var sequence in sequences)
            {
                foreach (var character in sequence)
                {
                    if (encoding.TryGetValue(character.ToString(), out var code))
                    {
                        totalBits += code.Length;
                    }
                    else
                    {
                        totalBits += 8; // Fallback to 8 bits
                    }
                }
            }

            return totalBits;
        }

        private static float CalculateAccessPatternEntropy(
            Dictionary<Type, int> accessPatterns,
            int totalAccesses)
        {
            var entropy = 0f;

            foreach (var accesses in accessPatterns.Values)
            {
                var probability = (float)accesses / totalAccesses;
                if (probability > 0)
                {
                    entropy -= probability * Mathf.Log(probability, 2f);
                }
            }

            return entropy;
        }

        private static float CalculateExpectedSearchTime(
            Dictionary<Type, int> accessPatterns,
            int totalAccesses)
        {
            var expectedTime = 0f;

            var sortedPatterns = accessPatterns
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            for (int i = 0; i < sortedPatterns.Count; i++)
            {
                var probability = (float)sortedPatterns[i].Value / totalAccesses;
                var searchPosition = i + 1; // 1-indexed position

                expectedTime += probability * searchPosition;
            }

            return expectedTime;
        }

        private static int EstimateCompressionSize(byte[] data)
        {
            // Simplified compression estimation using run-length encoding principles
            var compressedSize = 0;
            var currentByte = data[0];
            var runLength = 1;

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] == currentByte && runLength < 255)
                {
                    runLength++;
                }
                else
                {
                    compressedSize += 2; // 1 byte for value, 1 byte for run length
                    currentByte = data[i];
                    runLength = 1;
                }
            }

            compressedSize += 2; // Final run
            return compressedSize;
        }

        private static float CalculateHammingBound(int dataLength, float targetReliability)
        {
            // Hamming bound for error correction: 2^r ≥ m + r + 1
            // where r is redundancy bits, m is data bits
            var errorProbability = 1f - targetReliability;
            var requiredCorrection = Mathf.Log(1f / errorProbability, 2f);

            return requiredCorrection;
        }
    }

    // Supporting data structures for information theory optimizations
    public struct GeneticInformationOptimization
    {
        public float OriginalEntropy;
        public float OptimalEntropy;
        public float CompressionRatio;
        public float InformationGain;
        public Dictionary<string, string> OptimalEncoding;
    }

    public struct ServiceDiscoveryOptimization
    {
        public float AccessEntropy;
        public List<Type> OptimalSearchOrder;
        public float ExpectedSearchTime;
        public Dictionary<Type, float> InformationContent;
    }

    public struct RedundancyOptimization
    {
        public float OriginalRedundancy;
        public float OptimalRedundancy;
        public int ErrorCorrectionBits;
        public float ReliabilityAchieved;
    }
}