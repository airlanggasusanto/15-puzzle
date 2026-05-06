using System;
using System.Runtime.InteropServices;

public static class WindowAPI {
	// This code works exclusively with standalone build.
	// Executing GetActiveWindow in unity editor returns editor window.
	#if !UNITY_EDITOR

	private const int DefaultValue = -1;

	// Identifier of MINMAXINFO && SIZING message
	private const uint WM_GETMINMAXINFO = 0x0024;
	private const uint WM_SIZING = 0x0214;

	// SetWindowLongPtr argument : Sets a new address for the window procedure.
	private const int GWLP_WNDPROC = -4;

	private static int width;
	private static int height;
	private static float aspect = 0f;
	private static bool enabled;

	// Reference to current window
	private static HandleRef hMainWindow;

	// Reference to unity WindowsProcedure handler
	private static IntPtr unityWndProcHandler;

	// Reference to custom WindowsProcedure handler
	private static IntPtr customWndProcHandler;

	// Delegate signature for WindowsProcedure
	private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	// Instance of delegate
	private static WndProcDelegate procDelegate;

	[StructLayout(LayoutKind.Sequential)]
	private struct Minmaxinfo {
		public Point ptReserved;
		public Point ptMaxSize;
		public Point ptMaxPosition;
		public Point ptMinTrackSize;
		public Point ptMaxTrackSize;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT {
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Point {
		public int x;
		public int y;
	}
	#endif
	public static void SetAspectRatio(int w, int h)
	{
	#if !UNITY_EDITOR
		if (w <= 0 || h <= 0) throw new ArgumentException();
		aspect = (float)w / h;
	#endif
	}

	public static void ClearAspectRatio()
	{
	#if !UNITY_EDITOR
		aspect = 0f;
	#endif
	}


	public static void Set(int minWidth, int minHeight){
		#if !UNITY_EDITOR
		if (minWidth < 0 || minHeight < 0) throw new ArgumentException("Any component of min size cannot be less than 0");

		width = minWidth;
		height = minHeight;
			
		if(enabled) return;

		// Get reference
		hMainWindow = new HandleRef(null, GetActiveWindow());
		procDelegate = WndProc;
		// Generate handler
		customWndProcHandler = Marshal.GetFunctionPointerForDelegate(procDelegate);
		// Replace unity mesages handler with custom
		unityWndProcHandler = SetWindowLongPtr(hMainWindow, GWLP_WNDPROC, customWndProcHandler);
			
		enabled = true;
		#endif
	}

	public static void Reset(){
		#if !UNITY_EDITOR
		if(!enabled) return;
		// Replace custom message handler with unity handler
		SetWindowLongPtr(hMainWindow, GWLP_WNDPROC, unityWndProcHandler);
		hMainWindow = new HandleRef(null, IntPtr.Zero);
		unityWndProcHandler = IntPtr.Zero;
		customWndProcHandler = IntPtr.Zero;
		procDelegate = null;
		
		width = 0;
		height = 0;
			
		enabled = false;
		#endif
	}

	#if !UNITY_EDITOR

	private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam){
		// All messages except WM_GETMINMAXINFO & WM_SIZING will send to unity handler
		if (msg == WM_GETMINMAXINFO) {
			var x = (Minmaxinfo)Marshal.PtrToStructure(lParam, typeof(Minmaxinfo));
			x.ptMinTrackSize = new Point { x = width, y = height };
			Marshal.StructureToPtr(x, lParam, false);
			return DefWindowProc(hWnd, msg, wParam, lParam);
		}
		
		if (msg == WM_SIZING && aspect > 0) {
			RECT rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
			
			int currentWidth = rect.Right - rect.Left;
			int currentHeight = rect.Bottom - rect.Top;

			if (currentHeight > (currentWidth / aspect)) {
				rect.Bottom = rect.Top + (int)(currentWidth / aspect);
			}

			Marshal.StructureToPtr(rect, lParam, false);
			return (IntPtr)1; // Message handled
		}
		return CallWindowProc(unityWndProcHandler, hWnd, msg, wParam, lParam);
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll", EntryPoint = "CallWindowProcA")]
	private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint wMsg, IntPtr wParam,
		IntPtr lParam);

	[DllImport("user32.dll", EntryPoint = "DefWindowProcA")]
	private static extern IntPtr DefWindowProc(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

	private static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong){
		if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
		return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
	}

	[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
	private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
	private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);
	#endif
}