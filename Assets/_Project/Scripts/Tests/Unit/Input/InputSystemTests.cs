using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Unity.Mathematics;
using Laboratory.Core.Input.Interfaces;
using Laboratory.Core.Input.Services;
using Laboratory.Core.Input.Events;

namespace Laboratory.Tests.Unit.Input
{
    /// <summary>
    /// Unit tests for the enhanced input system components.
    /// </summary>
    public class InputSystemTests
    {
        private InputConfiguration _testConfiguration;
        private InputBuffer _inputBuffer;
        private InputValidator _inputValidator;
        private InputEventSystem _inputEventSystem;

        [SetUp]
        public void Setup()
        {
            // Initialize test configuration
            _testConfiguration = new InputConfiguration
            {
                LookSensitivity = 2.0f,
                InputDeadzone = 0.2f,
                InputBufferingEnabled = true,
                InputBufferTime = 1.0f
            };

            // Initialize subsystems
            _inputBuffer = new InputBuffer(_testConfiguration);
            _inputValidator = new InputValidator();
            _inputEventSystem = new InputEventSystem();
        }

        [TearDown]
        public void TearDown()
        {
            _inputBuffer?.ClearBuffer();
            _inputEventSystem?.ClearSubscriptions();
            InputEvents.ClearAllEvents();
        }

        #region InputConfiguration Tests

        [Test]
        public void InputConfiguration_LookSensitivity_ClampedToValidRange()
        {
            var config = new InputConfiguration();
            
            config.LookSensitivity = -1.0f;
            Assert.AreEqual(0.1f, config.LookSensitivity);
            
            config.LookSensitivity = 15.0f;
            Assert.AreEqual(10.0f, config.LookSensitivity);
            
            config.LookSensitivity = 5.0f;
            Assert.AreEqual(5.0f, config.LookSensitivity);
        }

        [Test]
        public void InputConfiguration_InputDeadzone_ClampedToValidRange()
        {
            var config = new InputConfiguration();
            
            config.InputDeadzone = -0.5f;
            Assert.AreEqual(0.0f, config.InputDeadzone);
            
            config.InputDeadzone = 1.5f;
            Assert.AreEqual(1.0f, config.InputDeadzone);
            
            config.InputDeadzone = 0.3f;
            Assert.AreEqual(0.3f, config.InputDeadzone);
        }

        [Test]
        public void InputConfiguration_InputBufferTime_ClampedToValidRange()
        {
            var config = new InputConfiguration();
            
            config.InputBufferTime = 0.05f;
            Assert.AreEqual(0.1f, config.InputBufferTime);
            
            config.InputBufferTime = 3.0f;
            Assert.AreEqual(2.0f, config.InputBufferTime);
            
            config.InputBufferTime = 0.8f;
            Assert.AreEqual(0.8f, config.InputBufferTime);
        }

        #endregion

        #region InputBuffer Tests

        [Test]
        public void InputBuffer_BufferInput_AddsInputCorrectly()
        {
            var actionName = "TestAction";
            var value = true;
            var timestamp = 1.0;

            _inputBuffer.BufferInput(actionName, value, timestamp);

            Assert.AreEqual(1, _inputBuffer.BufferedInputCount);
            Assert.IsTrue(_inputBuffer.WasInputBuffered(actionName, timestamp + 0.5));
        }

        [Test]
        public void InputBuffer_WasInputBuffered_ReturnsFalseForExpiredInput()
        {
            var actionName = "TestAction";
            var value = true;
            var timestamp = 1.0;

            _inputBuffer.BufferInput(actionName, value, timestamp);

            // Check within buffer time - should be true
            Assert.IsTrue(_inputBuffer.WasInputBuffered(actionName, timestamp + 0.5));

            // Check beyond buffer time - should be false
            Assert.IsFalse(_inputBuffer.WasInputBuffered(actionName, timestamp + 2.0));
        }

        [Test]
        public void InputBuffer_ConsumeBufferedInput_MarksInputAsConsumed()
        {
            var actionName = "TestAction";
            var value = true;
            var timestamp = 1.0;

            _inputBuffer.BufferInput(actionName, value, timestamp);
            
            Assert.AreEqual(1, _inputBuffer.BufferedInputCount);
            Assert.IsTrue(_inputBuffer.ConsumeBufferedInput(actionName));
            Assert.AreEqual(0, _inputBuffer.BufferedInputCount);
        }

        [Test]
        public void InputBuffer_Update_RemovesExpiredInputs()
        {
            var actionName = "TestAction";
            var value = true;
            var timestamp = 1.0;

            _inputBuffer.BufferInput(actionName, value, timestamp);
            Assert.AreEqual(1, _inputBuffer.BufferedInputCount);

            // Update with time beyond buffer time
            _inputBuffer.Update(timestamp + _testConfiguration.InputBufferTime + 0.1);
            Assert.AreEqual(0, _inputBuffer.BufferedInputCount);
        }

        [Test]
        public void InputBuffer_GetBufferedValue_ReturnsCorrectValue()
        {
            var actionName = "TestAction";
            var value = 5.5f;
            var timestamp = 1.0;

            _inputBuffer.BufferInput(actionName, value, timestamp);
            var retrievedValue = _inputBuffer.GetBufferedValue<float>(actionName);

            Assert.AreEqual(value, retrievedValue);
        }

        [Test]
        public void InputBuffer_ClearBuffer_RemovesAllInputs()
        {
            _inputBuffer.BufferInput("Action1", true, 1.0);
            _inputBuffer.BufferInput("Action2", false, 1.1);
            _inputBuffer.BufferInput("Action3", 2.5f, 1.2);

            Assert.AreEqual(3, _inputBuffer.BufferedInputCount);

            _inputBuffer.ClearBuffer();
            Assert.AreEqual(0, _inputBuffer.BufferedInputCount);
        }

        #endregion

        #region InputValidator Tests

        [Test]
        public void InputValidator_ValidateInputValue_Float_ValidatesCorrectly()
        {
            // Valid float values
            Assert.IsTrue(_inputValidator.ValidateInputValue(1.5f));
            Assert.IsTrue(_inputValidator.ValidateInputValue(0.0f));
            Assert.IsTrue(_inputValidator.ValidateInputValue(-10.0f));

            // Invalid float values
            Assert.IsFalse(_inputValidator.ValidateInputValue(float.NaN));
            Assert.IsFalse(_inputValidator.ValidateInputValue(float.PositiveInfinity));
            Assert.IsFalse(_inputValidator.ValidateInputValue(float.NegativeInfinity));
        }

        [Test]
        public void InputValidator_ValidateMovementInput_ValidatesCorrectly()
        {
            // Valid movement inputs
            Assert.IsTrue(_inputValidator.ValidateMovementInput(new float2(0.5f, 0.8f)));
            Assert.IsTrue(_inputValidator.ValidateMovementInput(new float2(1.0f, 0.0f)));
            Assert.IsTrue(_inputValidator.ValidateMovementInput(float2.zero));

            // Invalid movement inputs (NaN values)
            Assert.IsFalse(_inputValidator.ValidateMovementInput(new float2(float.NaN, 0.5f)));
            Assert.IsFalse(_inputValidator.ValidateMovementInput(new float2(0.5f, float.PositiveInfinity)));
        }

        [Test]
        public void InputValidator_IsActionAllowed_WithDisallowedActions()
        {
            var rules = new InputValidationRules
            {
                DisallowedActions = new[] { "Attack", "Jump" }
            };

            _inputValidator.SetValidationRules(rules);

            Assert.IsFalse(_inputValidator.IsActionAllowed("Attack"));
            Assert.IsFalse(_inputValidator.IsActionAllowed("Jump"));
            Assert.IsTrue(_inputValidator.IsActionAllowed("Move"));
            Assert.IsTrue(_inputValidator.IsActionAllowed("Interact"));
        }

        #endregion

        #region InputEventSystem Tests

        [Test]
        public void InputEventSystem_Subscribe_AddsCallback()
        {
            var callbackInvoked = false;
            var actionName = "TestAction";

            _inputEventSystem.Subscribe(actionName, (eventArgs) => {
                callbackInvoked = true;
            });

            var testEventArgs = new InputActionEventArgs
            {
                ActionName = actionName,
                Phase = UnityEngine.InputSystem.InputActionPhase.Performed,
                Value = true,
                Time = 1.0
            };

            _inputEventSystem.PublishEvent(testEventArgs);
            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public void InputEventSystem_Unsubscribe_RemovesCallback()
        {
            var callbackInvoked = false;
            var actionName = "TestAction";

            System.Action<InputActionEventArgs> callback = (eventArgs) => {
                callbackInvoked = true;
            };

            _inputEventSystem.Subscribe(actionName, callback);
            _inputEventSystem.Unsubscribe(actionName, callback);

            var testEventArgs = new InputActionEventArgs
            {
                ActionName = actionName,
                Phase = UnityEngine.InputSystem.InputActionPhase.Performed,
                Value = true,
                Time = 1.0
            };

            _inputEventSystem.PublishEvent(testEventArgs);
            Assert.IsFalse(callbackInvoked);
        }

        [Test]
        public void InputEventSystem_PublishEvent_InvokesSubscribedCallbacks()
        {
            var callback1Invoked = false;
            var callback2Invoked = false;
            var actionName = "TestAction";

            _inputEventSystem.Subscribe(actionName, (eventArgs) => {
                callback1Invoked = true;
            });

            _inputEventSystem.Subscribe(actionName, (eventArgs) => {
                callback2Invoked = true;
            });

            var testEventArgs = new InputActionEventArgs
            {
                ActionName = actionName,
                Phase = UnityEngine.InputSystem.InputActionPhase.Performed,
                Value = true,
                Time = 1.0
            };

            _inputEventSystem.PublishEvent(testEventArgs);
            
            Assert.IsTrue(callback1Invoked);
            Assert.IsTrue(callback2Invoked);
        }

        [Test]
        public void InputEventSystem_ClearSubscriptions_RemovesAllCallbacks()
        {
            var callbackInvoked = false;
            var actionName = "TestAction";

            _inputEventSystem.Subscribe(actionName, (eventArgs) => {
                callbackInvoked = true;
            });

            _inputEventSystem.ClearSubscriptions();

            var testEventArgs = new InputActionEventArgs
            {
                ActionName = actionName,
                Phase = UnityEngine.InputSystem.InputActionPhase.Performed,
                Value = true,
                Time = 1.0
            };

            _inputEventSystem.PublishEvent(testEventArgs);
            Assert.IsFalse(callbackInvoked);
        }

        #endregion

        #region InputEvents Static Tests

        [Test]
        public void InputEvents_MovementInput_TriggersCorrectly()
        {
            var eventTriggered = false;
            MovementInputEventArgs receivedArgs = default;

            InputEvents.OnMovementInput += (args) => {
                eventTriggered = true;
                receivedArgs = args;
            };

            var testArgs = new MovementInputEventArgs
            {
                Direction = new float2(0.5f, 0.8f),
                Magnitude = 0.94f,
                Timestamp = 2.5,
                DeviceName = "TestDevice"
            };

            InputEvents.TriggerMovementInput(testArgs);

            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(testArgs.Direction, receivedArgs.Direction);
            Assert.AreEqual(testArgs.Magnitude, receivedArgs.Magnitude, 0.001f);
            Assert.AreEqual(testArgs.Timestamp, receivedArgs.Timestamp, 0.001);
            Assert.AreEqual(testArgs.DeviceName, receivedArgs.DeviceName);
        }

        [Test]
        public void InputEvents_ActionPressed_TriggersCorrectly()
        {
            var eventTriggered = false;
            ActionInputEventArgs receivedArgs = default;

            InputEvents.OnActionPressed += (args) => {
                eventTriggered = true;
                receivedArgs = args;
            };

            var testArgs = new ActionInputEventArgs
            {
                ActionName = "TestAction",
                IsPressed = true,
                Timestamp = 1.5,
                DeviceName = "TestDevice",
                Value = 1.0f
            };

            InputEvents.TriggerActionPressed(testArgs);

            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(testArgs.ActionName, receivedArgs.ActionName);
            Assert.AreEqual(testArgs.IsPressed, receivedArgs.IsPressed);
            Assert.AreEqual(testArgs.Timestamp, receivedArgs.Timestamp, 0.001);
            Assert.AreEqual(testArgs.Value, receivedArgs.Value, 0.001f);
        }

        [Test]
        public void InputEvents_LongPress_TriggersCorrectly()
        {
            var startTriggered = false;
            var heldTriggered = false;
            var releasedTriggered = false;

            InputEvents.OnLongPressStarted += (args) => { startTriggered = true; };
            InputEvents.OnLongPressHeld += (args) => { heldTriggered = true; };
            InputEvents.OnLongPressReleased += (args) => { releasedTriggered = true; };

            var testArgs = new LongPressEventArgs
            {
                ActionName = "TestAction",
                PressTime = 0.6f,
                ThresholdTime = 0.5f,
                Timestamp = 1.0,
                DeviceName = "TestDevice"
            };

            InputEvents.TriggerLongPressStarted(testArgs);
            InputEvents.TriggerLongPressHeld(testArgs);
            InputEvents.TriggerLongPressReleased(testArgs);

            Assert.IsTrue(startTriggered);
            Assert.IsTrue(heldTriggered);
            Assert.IsTrue(releasedTriggered);
        }

        [Test]
        public void InputEvents_ClearAllEvents_RemovesAllSubscriptions()
        {
            var eventTriggered = false;

            // Subscribe to multiple events
            InputEvents.OnMovementInput += (args) => { eventTriggered = true; };
            InputEvents.OnActionPressed += (args) => { eventTriggered = true; };
            InputEvents.OnLongPressStarted += (args) => { eventTriggered = true; };

            // Clear all events
            InputEvents.ClearAllEvents();

            // Try to trigger events
            InputEvents.TriggerMovementInput(new MovementInputEventArgs());
            InputEvents.TriggerActionPressed(new ActionInputEventArgs());
            InputEvents.TriggerLongPressStarted(new LongPressEventArgs());

            // No events should have been triggered
            Assert.IsFalse(eventTriggered);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void InputSystem_DeadzoneProcessing_WorksCorrectly()
        {
            var deadzone = 0.2f;
            var config = new InputConfiguration { InputDeadzone = deadzone };

            // Test inputs below deadzone - should be zeroed
            var belowDeadzone = new float2(0.1f, 0.15f);
            var processedBelow = ApplyDeadzone(belowDeadzone, deadzone);
            Assert.AreEqual(float2.zero, processedBelow);

            // Test inputs above deadzone - should be scaled
            var aboveDeadzone = new float2(0.8f, 0.6f);
            var processedAbove = ApplyDeadzone(aboveDeadzone, deadzone);
            Assert.Greater(math.length(processedAbove), 0.01f);
            Assert.Less(math.length(processedAbove), math.length(aboveDeadzone));
        }

        [UnityTest]
        public IEnumerator InputSystem_BufferExpiration_WorksOverTime()
        {
            var actionName = "TestAction";
            var bufferTime = 0.5f;
            var config = new InputConfiguration { InputBufferTime = bufferTime };
            var buffer = new InputBuffer(config);

            // Add input to buffer
            buffer.BufferInput(actionName, true, Time.unscaledTime);
            Assert.AreEqual(1, buffer.BufferedInputCount);

            // Wait for buffer time + small margin
            yield return new WaitForSecondsRealtime(bufferTime + 0.1f);

            // Update buffer - should remove expired input
            buffer.Update(Time.unscaledTime);
            Assert.AreEqual(0, buffer.BufferedInputCount);
        }

        #endregion

        #region Helper Methods

        private float2 ApplyDeadzone(float2 input, float deadzone)
        {
            var magnitude = math.length(input);
            
            if (magnitude < deadzone)
            {
                return float2.zero;
            }
            
            var normalizedMagnitude = (magnitude - deadzone) / (1.0f - deadzone);
            return math.normalize(input) * normalizedMagnitude;
        }

        #endregion
    }

    /// <summary>
    /// Performance tests for input system components.
    /// </summary>
    public class InputSystemPerformanceTests
    {
        [Test, Performance]
        public void InputBuffer_BufferManyInputs_PerformanceTest()
        {
            var config = new InputConfiguration();
            var buffer = new InputBuffer(config);
            var inputCount = 1000;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < inputCount; i++)
            {
                buffer.BufferInput($"Action{i % 10}", true, i * 0.1);
            }

            stopwatch.Stop();

            Debug.Log($"Buffered {inputCount} inputs in {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 100); // Should complete in under 100ms
        }

        [Test, Performance]
        public void InputValidator_ValidateManyInputs_PerformanceTest()
        {
            var validator = new InputValidator();
            var inputCount = 10000;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < inputCount; i++)
            {
                var input = new float2(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                );
                validator.ValidateMovementInput(input);
            }

            stopwatch.Stop();

            Debug.Log($"Validated {inputCount} inputs in {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 50); // Should complete in under 50ms
        }
    }
}
