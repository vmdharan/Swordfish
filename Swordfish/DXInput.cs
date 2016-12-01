using SharpDX.DirectInput;

namespace Swordfish
{
    class DXInput
    {
        private DirectInput dInput;
        private Mouse mouse;
        private Keyboard keyboard;

        // Mouse state variables.
        public MouseState currMouseState;
        public MouseState prevMouseState;
        public long mouseX;
        public long mouseY;
        public long mouseZ;

        // Keyboard state variables.
        public KeyboardState currKeyboardState;
        public KeyboardState prevKeyboardState;

        // Screen size
        public float formHeight;
        public float formWidth;

        // Constructor
        public DXInput()
        {
            dInput = new DirectInput();
            mouse = new Mouse(dInput);
            keyboard = new Keyboard(dInput);

            mouse.Acquire();
            keyboard.Acquire();

            currMouseState = new MouseState();
            prevMouseState = new MouseState();

            currKeyboardState = new KeyboardState();
            prevKeyboardState = new KeyboardState();

            mouseX = mouseY = mouseZ = 0;
        }

        public void Update()
        {
            prevKeyboardState = currKeyboardState;
            currKeyboardState = keyboard.GetCurrentState();

            prevMouseState = currMouseState;
            currMouseState = mouse.GetCurrentState();

            if (mouseX < 0) { mouseX = 0; }
            if (mouseY < 0) { mouseY = 0; }
            if (mouseZ < 0) { mouseZ = 0; }
            //if (mouseX > formWidth) { mouseX = (long) formWidth; }
            //if (mouseY > formHeight) { mouseY = (long) formHeight; }
            if (mouseZ > 1) { mouseZ = (long)1.0; }

        }

        // Set the screen dimensions.
        public void setDimensions(int w, int h)
        {
            formWidth = (float)w;
            formHeight = (float)h;
        }

        // Release access to resources.
        public void CleanUp()
        {
            mouse.Unacquire();
            mouse.Dispose();
            keyboard.Unacquire();
            keyboard.Dispose();
        }
    }
}
