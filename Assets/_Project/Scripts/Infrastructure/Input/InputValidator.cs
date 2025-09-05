using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Infrastructure.Input
{
    /// <summary>
    /// Input validator for the unified input system.
    /// Applies deadzones, normalizes values, and filters invalid inputs.
    /// </summary>
    public class InputValidator
    {
        private readonly InputConfiguration _configuration;
        
        public InputValidator(InputConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        /// <summary>
        /// Validates and processes a 2D movement input (like WASD movement).
        /// </summary>
        public float2 ValidateMovementInput(float2 input)
        {
            // Apply deadzone
            if (math.length(input) < _configuration.inputDeadzone)
            {
                return float2.zero;
            }
            
            // Normalize if length exceeds 1
            if (math.length(input) > 1.0f)
            {
                input = math.normalize(input);
            }
            
            return input;
        }
        
        /// <summary>
        /// Validates and processes a 2D look input (like mouse movement).
        /// </summary>
        public float2 ValidateLookInput(float2 input)
        {
            // Apply look-specific deadzone
            if (math.length(input) < _configuration.lookDeadzone)
            {
                return float2.zero;
            }
            
            // Apply sensitivity
            input *= _configuration.lookSensitivity;
            
            return input;
        }
        
        /// <summary>
        /// Validates a generic float input value.
        /// </summary>
        public float ValidateFloatInput(float input, float deadzone = 0.1f)
        {
            return math.abs(input) < deadzone ? 0f : input;
        }
        
        /// <summary>
        /// Validates a boolean input (button press).
        /// </summary>
        public bool ValidateBoolInput(bool input)
        {
            // For buttons, no special validation needed
            return input;
        }
        
        /// <summary>
        /// Checks if an input value represents a significant change.
        /// </summary>
        public bool HasSignificantChange(float2 current, float2 previous, float threshold = 0.01f)
        {
            return math.length(current - previous) > threshold;
        }
    }
}
