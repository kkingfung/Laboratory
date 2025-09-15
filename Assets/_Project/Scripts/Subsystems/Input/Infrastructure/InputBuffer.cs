using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Infrastructure.Input
{
    /// <summary>
    /// Input buffering for the unified input system.
    /// Stores recent inputs to allow for more forgiving input timing.
    /// </summary>
    public class InputBuffer
    {
        private readonly Dictionary<string, BufferedInput> _bufferedInputs = new();
        private readonly float _bufferTime;
        
        public InputBuffer(float bufferTime = 0.5f)
        {
            _bufferTime = bufferTime;
        }
        
        /// <summary>
        /// Buffers an input with a timestamp.
        /// </summary>
        public void BufferInput(string inputName, float timestamp)
        {
            _bufferedInputs[inputName] = new BufferedInput
            {
                InputName = inputName,
                Timestamp = timestamp,
                IsConsumed = false
            };
        }
        
        /// <summary>
        /// Checks if an input is available in the buffer within the buffer time.
        /// </summary>
        public bool TryConsumeBufferedInput(string inputName, float currentTime)
        {
            if (_bufferedInputs.TryGetValue(inputName, out var bufferedInput))
            {
                if (!bufferedInput.IsConsumed && (currentTime - bufferedInput.Timestamp) <= _bufferTime)
                {
                    bufferedInput.IsConsumed = true;
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Clears expired inputs from the buffer.
        /// </summary>
        public void Update(float currentTime)
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _bufferedInputs)
            {
                if ((currentTime - kvp.Value.Timestamp) > _bufferTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _bufferedInputs.Remove(key);
            }
        }
        
        /// <summary>
        /// Clears all buffered inputs.
        /// </summary>
        public void ClearBuffer()
        {
            _bufferedInputs.Clear();
        }
        
        private class BufferedInput
        {
            public string InputName { get; set; } = "";
            public float Timestamp { get; set; }
            public bool IsConsumed { get; set; }
        }
    }
}
