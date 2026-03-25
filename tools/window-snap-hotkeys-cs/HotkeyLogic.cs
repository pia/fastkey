using System;

namespace WindowSnapHotkeys
{
    internal enum SnapSide
    {
        Left,
        Right
    }

    internal struct HotkeyProcessingResult
    {
        public HotkeyProcessingResult(bool shouldConsumeEvent, SnapSide? triggeredSide)
        {
            ShouldConsumeEvent = shouldConsumeEvent;
            TriggeredSide = triggeredSide;
        }

        public bool ShouldConsumeEvent;
        public SnapSide? TriggeredSide;
    }

    internal sealed class HotkeyStateMachine
    {
        private const int VkA = 0x41;
        private const int VkD = 0x44;
        private const int VkMenu = 0x12;

        private bool _leftHandled;
        private bool _rightHandled;

        public HotkeyProcessingResult ProcessKeyEvent(bool isKeyDown, bool isKeyUp, int vkCode, bool isAltPressed)
        {
            if (isKeyDown && isAltPressed)
            {
                if (vkCode == VkA)
                {
                    if (_leftHandled)
                    {
                        return new HotkeyProcessingResult(true, null);
                    }

                    _leftHandled = true;
                    return new HotkeyProcessingResult(true, SnapSide.Left);
                }

                if (vkCode == VkD)
                {
                    if (_rightHandled)
                    {
                        return new HotkeyProcessingResult(true, null);
                    }

                    _rightHandled = true;
                    return new HotkeyProcessingResult(true, SnapSide.Right);
                }
            }

            if (isKeyUp)
            {
                if (vkCode == VkA && _leftHandled)
                {
                    _leftHandled = false;
                    return new HotkeyProcessingResult(true, null);
                }

                if (vkCode == VkD && _rightHandled)
                {
                    _rightHandled = false;
                    return new HotkeyProcessingResult(true, null);
                }

                if (vkCode == VkMenu)
                {
                    _leftHandled = false;
                    _rightHandled = false;
                }
            }

            return new HotkeyProcessingResult(false, null);
        }
    }
}
