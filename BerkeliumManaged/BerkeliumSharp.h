// BerkeliumManaged.h

#pragma once

#using <mscorlib.dll>

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

namespace Berkelium {
  namespace Managed {

    class WindowDelegateWrapper;

    ref class ProtocolHandler;
    ref class Context;
    ref class Widget;
    ref class Window;
    ref struct Data;
    ref struct Rect;

    class ErrorDelegateWrapper : public ::Berkelium::ErrorDelegate {
    public:
      ErrorDelegateWrapper () {
      }

      virtual void onPureCall();
      virtual void onInvalidParameter(const wchar_t *expression, const wchar_t *function, const wchar_t *file, unsigned int line, uintptr_t reserved);
      virtual void onOutOfMemory();
      virtual void onAssertion(const char *assertMessage);
    };

    public delegate void ErrorHandler ();
    public delegate void AssertionHandler (System::String ^ message);
    public delegate void InvalidParameterHandler (System::String ^ expression, System::String ^ function, System::String ^ file, int lineNumber);

    public ref class BerkeliumSharp abstract sealed {
    internal:
      static bool IsInitialized;
      static ErrorDelegateWrapper * Wrapper;

    public:
      static event ErrorHandler ^ PureCall;
      static event ErrorHandler ^ OutOfMemory;
      static event AssertionHandler ^ Assertion;
      static event InvalidParameterHandler ^ InvalidParameter;

    internal:
      static void OnPureCall () {
        PureCall();
      }

      static void OnOutOfMemory () {
        OutOfMemory();
      }

      static void OnAssertion (System::String ^ assertMessage) {
        Assertion(assertMessage);
      }

      static void OnInvalidParameter (System::String ^ expression, System::String ^ function, System::String ^ file, int lineNumber) {
        InvalidParameter(expression, function, file, lineNumber);
      }

    public:
      /// <summary>
      /// Initializes the Berkelium library for the current process, specifying a home directory to use for browser cache, preferences, and data.
      /// </summary>
      static void Init (System::String ^ homeDirectory);

      /// <summary>
      /// Initializes the Berkelium library for the current process, using the default home directory for browser cache, preferences, and data.
      /// </summary>
      static void Init () {
        Init(nullptr);
      }

      /// <summary>
      /// Cleans up the Berkelium library for the current process. Note that this function must only be called once per process.
      /// </summary>
      static void Destroy () {
        if (!IsInitialized)
          return;

        ::Berkelium::setErrorHandler(0);
        ::Berkelium::destroy();
        if (Wrapper) {
          delete Wrapper;
          Wrapper = 0;
        }
        IsInitialized = false;
      }

      /// <summary>
      /// Runs the Berkelium message pump, processing any pending messages or tasks and dispatching events.
      /// </summary>
      static void Update () {
        if (!IsInitialized)
          return;

        ::Berkelium::update();
      }
    };

    public enum class MouseButton : System::UInt32  {
      Left = 0,
      Middle = 1,
      Right = 2
    };

    [FlagsAttribute] 
    public enum class KeyModifier : System::Int32 {
      SHIFT_MOD = 1 << 0,
      CONTROL_MOD = 1 << 1,
      ALT_MOD = 1 << 2,
      META_MOD = 1 << 3,
      KEYPAD_KEY = 1 << 4,
      AUTOREPEAT_KEY = 1 << 5,
      SYSTEM_KEY = 1 << 6
    };

    [FlagsAttribute] 
    public enum class ScriptAlertFlags : System::Int32 {
      HasOKButton = 0x1,
      HasCancelButton = 0x2,
      HasPromptField = 0x4,
      HasMessage = 0x8
    };

    public enum class MediaType : System::Int32  {
      None,
      Image,
      Video,
      Audio,
    };

    [FlagsAttribute] 
    public enum class EditFlags : System::Int32 {
      CanDoNone = 0x0,
      CanUndo = 0x1,
      CanRedo = 0x2,
      CanCut = 0x4,
      CanCopy = 0x8,
      CanPaste = 0x10,
      CanDelete = 0x20,
      CanSelectAll = 0x40,
    };

    public enum class ZoomFunction : System::Int32 {
      ZoomOut = -1,
      ResetZoom = 0,
      ZoomIn = 1
    };

    public ref struct ContextMenuEventArgs {
      MediaType MediaType;

      int MouseX, MouseY;

      System::String ^ LinkUrl, ^ SrcUrl, ^ PageUrl, ^ FrameUrl;

      System::String ^ SelectedText;

      bool IsEditable;
      EditFlags EditFlags;
    };

    public delegate void BasicHandler (Window ^ window);
    public delegate void AddressBarChangedHandler (Window ^ window, System::String ^ newUrl);
    public delegate void StartLoadingHandler (Window ^ window, System::String ^ newUrl);
    public delegate void LoadingStateChangedHandler (Window ^ window, bool isLoading);
    public delegate void ProvisionalLoadErrorHandler (Window ^ window, System::String ^ url, int errorCode, bool isMainFrame);
    public delegate void ChromeSendHandler (Window ^ window, System::String ^ message, array<System::String ^> ^ arguments);
    public delegate void CreatedWindowHandler (Window ^ window, Window ^ newWindow, Rect ^ initialRect, System::String ^ creatorUrl);
    public delegate void PaintHandler (Window ^ window, IntPtr sourceBuffer, Rect ^ rect, int dx, int dy, Rect ^ scrollRect);
    public delegate void CrashedPluginHandler (Window ^ window, System::String ^ pluginName);
    public delegate void ConsoleMessageHandler (Window ^ window, System::String ^ sourceId, System::String ^ message, int lineNumber);
    public delegate void ScriptAlertHandler (Window ^ window, System::String ^ message, System::String ^ defaultPrompt, System::String ^ url, ScriptAlertFlags flags, bool % success, System::String ^% prompt);
    public delegate void NavigationRequestedHandler (Window ^ window, System::String ^ newUrl, System::String ^ referrer, bool isNewWindow, bool % cancelDefaultAction);
    public delegate void TitleChangedHandler (Window ^ window, System::String ^ newTitle);
    public delegate void TooltipChangedHandler (Window ^ window, System::String ^ newTooltip);
    public delegate void WidgetCreatedHandler (Window ^ window, Widget ^ newWidget, int zIndex);
    public delegate void WidgetPaintHandler (Window ^ window, Widget ^ widget, IntPtr sourceBuffer, Rect ^ rect, int dx, int dy, Rect ^ scrollRect);
    public delegate void WidgetMovedHandler (Window ^ window, Widget ^ widget, int newX, int newY);
    public delegate void WidgetResizedHandler (Window ^ window, Widget ^ widget, int newWidth, int newHeight);
    public delegate void WidgetDestroyedHandler (Window ^ window, Widget ^ widget);
    public delegate void ShowContextMenuHandler (Window ^ window, ContextMenuEventArgs ^ args);
    public delegate void CursorChangedHandler (Window ^ window, IntPtr cursorHandle);

    class NativeProtocolHandler : public ::Berkelium::Protocol {
    public:
      gcroot<ProtocolHandler ^> Owner;

      NativeProtocolHandler (ProtocolHandler ^ owner) 
        : Owner(owner) {
      }

      bool HandleRequest (const wchar_t * url, size_t urlLength, HGLOBAL &responseBody, HGLOBAL &responseHeaders);
    };

    public ref class ProtocolHandler abstract {
    internal:
      NativeProtocolHandler * Native;
      System::String ^ Scheme;
      Managed::Context ^ Context;

      bool DoHandleRequest (const wchar_t * url, size_t urlLength, HGLOBAL &responseBody, HGLOBAL &responseHeaders);
    protected:
      /// <summary>
      /// Handles an incoming request for a custom protocol.
      /// </summary>
      /// <param name="url">Specifies the URL being requested.</param>
      /// <param name="responseBody">On a successful request, return the response body via this parameter.</param>
      /// <param name="responseHeaders">Return the response headers via this parameter. Each header line should be an individual string within the array.</param>
      /// <returns>true if the request was successful, false otherwise.</returns>
      virtual bool HandleRequest (System::String ^ url, array<unsigned char> ^% responseBody, array<System::String ^> ^% responseHeaders) = 0;
    public:
      /// <summary>
      /// Constructs and registers a custom protocol handler for the specified context and URL scheme.
      /// </summary>
      /// <param name="context">Specifies the context to register the handler for.</param>
      /// <param name="scheme">Specifies the URL scheme to register (omit the trailing colon.)</param>
      ProtocolHandler (Managed::Context ^ context, System::String ^ scheme);
      ~ProtocolHandler ();
    };

    class ContextTable : public std::map<::Berkelium::Context *, gcroot<Context ^>> {
    };

    public ref class Context {
    internal:
      static ContextTable * Table = 0;

      bool OwnsHandle;
      ::Berkelium::Context * Native;

      Context (::Berkelium::Context * native, bool ownsHandle)
        : Native(native)
        , OwnsHandle(ownsHandle) {
      }

      static Context ^ GetContext (::Berkelium::Context * pointer, bool ownsHandle);
      static bool ContextDestroyed (::Berkelium::Context * pointer);

    public:
      static Context ^ Create () {
        return GetContext(::Berkelium::Context::create(), true);
      }

      Context ^ Clone () {
        return GetContext(Native->clone(), true);
      }

      ~Context () {
        if (Native && OwnsHandle) {
            ContextDestroyed(Native);
            delete Native;
        }

        Native = 0;
      }

      virtual String^ ToString () override {
        return String::Format(
          "Context({0})", IntPtr((void*)Native).ToString()
        );
      }

    };

    public ref struct Rect {
    public:
      int Left, Top, Width, Height;

      Rect (int left, int top, int width, int height) 
        : Left(left)
        , Top(top) 
        , Width(width)
        , Height(height) {
      }

      property int Right {
        int get () {
          return Left + Width;
        }
      }

      property int Bottom {
        int get () {
          return Top + Height;
        }
      }

      virtual String^ ToString() override {
        return System::String::Format(
          "Rect({0},{1} {2}x{3})", 
          Left, Top, Width, Height
          );
      }
    };

    public ref class Widget {
    internal:
      bool OwnsHandle;
      Window ^ Parent;
      ::Berkelium::Widget * Native;

      Widget (Window ^ parent, ::Berkelium::Widget * native, bool ownsHandle) 
        : Parent(parent)
        , Native(native)
        , OwnsHandle(ownsHandle) {
      }

    public:
      event WidgetPaintHandler ^ Paint;
      event WidgetMovedHandler ^ Moved;
      event WidgetResizedHandler ^ Resized;
      event WidgetDestroyedHandler ^ Destroyed;

      ~Widget () {
        if (Native && OwnsHandle)
          delete Native;

        Native = 0;
      }

      property int Id {
        int get() {
          return Native->getId();
        }
      }

      property Window ^ ParentWindow {
        Window ^ get() {
          return Parent;
        }
      }

      /// <summary>
      /// Returns the virtual boundaries of the widget (relative to the parent window).
      /// </summary>
      property Berkelium::Managed::Rect ^ Rect {
        Berkelium::Managed::Rect ^ get() {
          ::Berkelium::Rect rect = Native->getRect();
          return gcnew Berkelium::Managed::Rect(rect.mLeft, rect.mTop, rect.mWidth, rect.mHeight);
        }
      }

      /// <summary>
      /// Generates a virtual keyboard event within the widget.
      /// </summary>
      /// <param name="pressed">Specifies whether the event represents a key press or key release event.</param>
      /// <param name="modifiers">Specifies the modifiers that affect the key event, if any.</param>
      /// <param name="vk_code">Specifies the virtual key code of the key event.</param>
      /// <param name="scancode">Specifies the keyboard scan code of the key event.</param>
      void KeyEvent (bool pressed, KeyModifier modifiers, int vk_code, int scancode) {
        Native->keyEvent(pressed, (int)modifiers, vk_code, scancode);
      }

      /// <summary>
      /// Generates a virtual text input event within the widget.
      /// </summary>
      /// <param name="text">Specifies the unicode character(s) generated by the keystrokes that produced the event.</param>
      void TextEvent (System::String ^ text) {
        pin_ptr<const wchar_t> textPtr = PtrToStringChars(text);

        Native->textEvent(textPtr, text->Length);
      }

      /// <summary>
      /// Generates a virtual mouse input event within the widget.
      /// </summary>
      /// <param name="buttonId">Specifies the mouse button that generated the event.</param>
      /// <param name="pressed">Specifies whether the event is a mouse down event or a mouse up event.</param>
      void MouseButton (MouseButton buttonId, bool pressed) {
        Native->mouseButton((unsigned)buttonId, pressed);
      }

      /// <summary>
      /// Generates a virtual mouse move event within the widget.
      /// </summary>
      /// <param name="x">Specifies the new X coordinate of the mouse cursor (relative to the top-left corner of the widget).</param>
      /// <param name="y">Specifies the new Y coordinate of the mouse cursor (relative to the top-left corner of the widget).</param>
      void MouseMoved (int x, int y) {
        Native->mouseMoved(x, y);
      }

      /// <summary>
      /// Generates a virtual mouse wheel event within the widget.
      /// </summary>
      void MouseWheel (int xScroll, int yScroll) {
        Native->mouseWheel(xScroll, yScroll);
      }

      /// <summary>
      /// Generates a virtual focus gained event within the widget.
      /// </summary>
      void Focus () {
        Native->focus();
      }

      /// <summary>
      /// Generates a virtual focus lost event within the widget.
      /// </summary>
      void Unfocus () {
        Native->unfocus();
      }

      /// <summary>
      /// Changes the window's position relative to the parent window. Slightly useless!
      /// </summary>
      void Move (int newX, int newY) {
        Native->setPos(newX, newY);
      }

      virtual String^ ToString() override {
        return System::String::Format(
          "Widget({0})", 
          Native->getId()
        );
      }

    internal:
      void OnDestroyed () {
        Destroyed(Parent, this);
      }

      void OnPaint (IntPtr sourceBuffer, Berkelium::Managed::Rect ^ rect, int dx, int dy, Berkelium::Managed::Rect ^ scrollRect) {
        Paint(Parent, this, sourceBuffer, rect, dx, dy, scrollRect);
      }

      void OnMoved (int newX, int newY) {
        Moved(Parent, this, newX, newY);
      }

      void OnResized (int newWidth, int newHeight) {
        Resized(Parent, this, newWidth, newHeight);
      }

    };

    typedef std::map<::Berkelium::Widget *, gcroot<Widget ^>> TWidgetTable;

    class WindowDelegateWrapper : public ::Berkelium::WindowDelegate {
    private:
      TWidgetTable WidgetTable;
    public:
      gcroot<Window ^> Owner;

      WindowDelegateWrapper (Window ^ owner)
        : Owner(owner) {
      }

      Widget ^ GetWidget (::Berkelium::Widget * widget, bool ownsHandle);
      bool WidgetDestroyed (::Berkelium::Widget * widget);

      virtual void onAddressBarChanged (::Berkelium::Window *win, const wchar_t *newURL, size_t newURLSize);
      virtual void onStartLoading (::Berkelium::Window *win, const wchar_t *newURL, size_t newURLSize);
      virtual void onLoad (::Berkelium::Window *win);
      virtual void onCrashed (::Berkelium::Window *win);
      virtual void onUnresponsive (::Berkelium::Window *win);
      virtual void onResponsive (::Berkelium::Window *win);
      virtual void onChromeSend (::Berkelium::Window *win, ::Berkelium::WindowDelegate::Data message, const ::Berkelium::WindowDelegate::Data *content, size_t numContents);
      virtual void onCreatedWindow (::Berkelium::Window *win, ::Berkelium::Window *newWindow, ::Berkelium::Rect &initialRect, const wchar_t *url, size_t urlLength);
      virtual void onPaint (::Berkelium::Window *win, const unsigned char *sourceBuffer, const ::Berkelium::Rect &rect, int dx, int dy, const ::Berkelium::Rect &scrollRect);
      virtual void onWidgetCreated (::Berkelium::Window *win, ::Berkelium::Widget *newWidget, int zIndex);
      virtual void onWidgetDestroyed (::Berkelium::Window *win, ::Berkelium::Widget *newWidget);
      virtual void onWidgetResize (::Berkelium::Window *win, ::Berkelium::Widget *wid, int newWidth, int newHeight);
      virtual void onWidgetMove (::Berkelium::Window *win, ::Berkelium::Widget *wid, int newX, int newY);
      virtual void onWidgetPaint (::Berkelium::Window *win, ::Berkelium::Widget *wid, const unsigned char *sourceBuffer, const ::Berkelium::Rect &rect, int dx, int dy, const ::Berkelium::Rect &scrollRect);
      virtual void onProvisionalLoadError (::Berkelium::Window *win, const wchar_t * url, size_t urlLength, int errorCode, bool isMainFrame);
      virtual void onCrashedWorker(::Berkelium::Window *win);
      virtual void onCrashedPlugin(::Berkelium::Window *win, const wchar_t *pluginName, size_t pluginNameLength);
      virtual void onConsoleMessage(::Berkelium::Window *win, const wchar_t *sourceId, size_t sourceIdLength, const wchar_t *message, size_t messageLength, int line_no);
      virtual void onScriptAlert(::Berkelium::Window *win, const wchar_t *message, size_t messageLength, const wchar_t *defaultValue, size_t defaultPromptLength, const wchar_t *url, size_t urlLength, int flags, bool &success, std::wstring &value);
      virtual void onNavigationRequested(::Berkelium::Window *win, const wchar_t *newUrl, size_t newUrlLength, const wchar_t *referrer, size_t referrerLength, bool isNewWindow, bool &cancelDefaultAction);
      virtual void onLoadingStateChanged(::Berkelium::Window *win, bool isLoading);
      virtual void onTitleChanged(::Berkelium::Window *win, const wchar_t *title, size_t titleLength);
      virtual void onTooltipChanged(::Berkelium::Window *win, const wchar_t *tooltip, size_t tooltipLength);
      virtual void onShowContextMenu(::Berkelium::Window *win, const ::Berkelium::ContextMenuEventArgs& args);
      virtual void onCursorUpdated(const Cursor& newCursor);
    };

    public ref class Window {
    internal:
      bool OwnsHandle;
      ::Berkelium::Window * Native;
      Context ^ ManagedContext;
      WindowDelegateWrapper * Wrapper;

      Window (Berkelium::Managed::Context ^ context, ::Berkelium::Window * native, bool ownsHandle)
        : Native(native)
        , OwnsHandle(ownsHandle)
        , ManagedContext(context) {

          if (ownsHandle) {
            Wrapper = new WindowDelegateWrapper(this);
            Native->setDelegate(Wrapper);
          }
      }

    public:
      event AddressBarChangedHandler ^ AddressBarChanged;
      event StartLoadingHandler ^ StartLoading;
      event BasicHandler ^ Load;
      event ProvisionalLoadErrorHandler ^ ProvisionalLoadError;
      event BasicHandler ^ Crashed;
      event BasicHandler ^ Unresponsive;
      event BasicHandler ^ Responsive;
      event ChromeSendHandler ^ ChromeSend;
      event CreatedWindowHandler ^ CreatedWindow;
      event PaintHandler ^ Paint;
      event BasicHandler ^ CrashedWorker;
      event CrashedPluginHandler ^ CrashedPlugin;
      event ConsoleMessageHandler ^ ConsoleMessage;
      event ScriptAlertHandler ^ ScriptAlert;
      event NavigationRequestedHandler ^ NavigationRequested;
      event WidgetCreatedHandler ^ WidgetCreated;
      event WidgetPaintHandler ^ WidgetPaint;
      event WidgetMovedHandler ^ WidgetMoved;
      event WidgetResizedHandler ^ WidgetResized;
      event WidgetDestroyedHandler ^ WidgetDestroyed;
      event LoadingStateChangedHandler ^ LoadingStateChanged;
      event TitleChangedHandler ^ TitleChanged;
      event TooltipChangedHandler ^ TooltipChanged;
      event ShowContextMenuHandler ^ ShowContextMenu;
      event CursorChangedHandler ^ CursorChanged;

      Window (Berkelium::Managed::Context ^ context)
        : Native(::Berkelium::Window::create(context->Native))
        , OwnsHandle(true)
        , ManagedContext(context) {

        Wrapper = new WindowDelegateWrapper(this);
        Native->setDelegate(Wrapper);
      }

      ~Window () {
        if (Native && OwnsHandle && BerkeliumSharp::IsInitialized)
          delete Native;
        if (Wrapper)
          delete Wrapper;

        Native = 0;
        Wrapper = 0;
      }

      virtual String^ ToString() override {
        return System::String::Format(
          "Window({0})", 
          Native->getId()
        );
      }

      property int Width {
        int get() {
          return Native->getWidget()->getRect().width();
        }
      }

      property int Height {
        int get() {
          return Native->getWidget()->getRect().height();
        }
      }

      property bool Focused;

      property int Id {
        int get() {
          return Native->getId();
        }
      }

      property Context ^ Context {
        Berkelium::Managed::Context ^ get () {
          return ManagedContext;
        }
      }

      /// <summary>
      /// The widget that represents the content area of the window.
      /// </summary>
      property Widget ^ Widget {
        Berkelium::Managed::Widget ^ get () {
          return Wrapper->GetWidget(Native->getWidget(), false);
        }
      }

      /// <summary>
      /// Determines whether the window background is opaque. If true, the window will have a usable alpha channel for overlay purposes.
      /// </summary>
      property bool Transparent {
        void set (bool isTransparent) {
          Native->setTransparent(isTransparent);
        }
      }

      /// <summary>
      /// Indicates whether it is possible to navigate backward in the window's history.
      /// </summary>
      property bool CanGoBack {
        bool get () {
          return Native->canGoBack();
        }
      }

      /// <summary>
      /// Indicates whether it is possible to navigate forward in the window's history.
      /// </summary>
      property bool CanGoForward {
        bool get () {
          return Native->canGoForward();
        }
      }

      /// <summary>
      /// Returns the topmost widget located at the specified position, if any.
      /// </summary>
      Berkelium::Managed::Widget ^ GetWidgetAtPoint (int x, int y) {
        return GetWidgetAtPoint(x, y, false);
      }

      /// <summary>
      /// Returns the topmost widget located at the specified position, if any.
      /// </summary>
      Berkelium::Managed::Widget ^ GetWidgetAtPoint (int x, int y, bool returnRootIfOutside) {
        ::Berkelium::Widget * ptr = Native->getWidgetAtPoint(x, y, returnRootIfOutside);
        if (ptr)
          return Wrapper->GetWidget(ptr, false);
        else
          return nullptr;
      }

      /// <summary>
      /// Generates a virtual keyboard event within the window.
      /// </summary>
      /// <param name="pressed">Specifies whether the event represents a key press or key release event.</param>
      /// <param name="modifiers">Specifies the modifiers that affect the key event, if any.</param>
      /// <param name="vk_code">Specifies the virtual key code of the key event.</param>
      /// <param name="scancode">Specifies the keyboard scan code of the key event.</param>
      void KeyEvent (bool pressed, KeyModifier modifiers, int vk_code, int scancode) {
        Native->keyEvent(pressed, (int)modifiers, vk_code, scancode);
      }

      /// <summary>
      /// Generates a virtual text input event within the window.
      /// </summary>
      /// <param name="text">Specifies the unicode character(s) generated by the keystrokes that produced the event.</param>
      void TextEvent (System::String ^ text) {
        pin_ptr<const wchar_t> textPtr = PtrToStringChars(text);

        Native->textEvent(textPtr, text->Length);
      }

      /// <summary>
      /// Generates a virtual mouse input event within the window.
      /// </summary>
      /// <param name="buttonId">Specifies the mouse button that generated the event.</param>
      /// <param name="pressed">Specifies whether the event is a mouse down event or a mouse up event.</param>
      void MouseButton (MouseButton buttonId, bool pressed) {
        Native->mouseButton((unsigned)buttonId, pressed);
      }

      /// <summary>
      /// Generates a virtual mouse move event within the window.
      /// </summary>
      /// <param name="x">Specifies the new X coordinate of the mouse cursor (relative to the top-left corner of the window).</param>
      /// <param name="y">Specifies the new Y coordinate of the mouse cursor (relative to the top-left corner of the window).</param>
      void MouseMoved (int x, int y) {
        Native->mouseMoved(x, y);
      }

      /// <summary>
      /// Generates a virtual mouse wheel event within the window.
      /// </summary>
      void MouseWheel (int xScroll, int yScroll) {
        Native->mouseWheel(xScroll, yScroll);
      }

      /// <summary>
      /// Asks the window to navigate to the specified URL.
      /// </summary>
      /// <param name="url">The URL to navigate to. Must be a fully formed URL including scheme.</param>
      /// <returns>true if the navigation was started successfully.</returns>
      virtual bool NavigateTo (System::String ^ url) {
        pin_ptr<const wchar_t> urlPtr = PtrToStringChars(url);

        return Native->navigateTo(urlPtr, url->Length);
      }

      /// <summary>
      /// Attempts to navigate backward in the window's history.
      /// </summary>
      void GoBack () {
        Native->goBack();
      }

      /// <summary>
      /// Attempts to navigate forward in the window's history.
      /// </summary>
      void GoForward () {
        Native->goForward();
      }

      /// <summary>
      /// Adjusts the zoom of the window.
      /// </summary>
      /// <param name="mode">Specifies how to adjust the zoom.</param>
      void AdjustZoom (ZoomFunction mode) {
        Native->adjustZoom((int)mode);
      }

      /// <summary>
      /// Executes a javascript snippet within the context of the window's currently loaded page (if any).
      /// </summary>
      /// <param name="javascript">The javascript to execute.</param>
      void ExecuteJavascript (System::String ^ javascript) {
        pin_ptr<const wchar_t> scriptPtr = PtrToStringChars(javascript);

        Native->executeJavascript(scriptPtr, javascript->Length);
      }

      /// <summary>
      /// Inserts a new CSS stylesheet within the context of the window's currently loaded page, optionally specifying an element to contain the CSS.
      /// </summary>
      /// <param name="css">The contents of the CSS stylesheet.</param>
      /// <param name="id">The ID of the element to contain the CSS, or null for none.</param>
      void InsertCSS (System::String ^ css, System::String ^ id) {
        pin_ptr<const wchar_t> cssPtr = PtrToStringChars(css);
        
        if (id == nullptr)
          Native->insertCSS(cssPtr, css->Length, 0, 0);
        else {
          pin_ptr<const wchar_t> idPtr = PtrToStringChars(id);
          Native->insertCSS(cssPtr, css->Length, idPtr, id->Length);
        }
      }

      /// <summary>
      /// Reloads the currently loaded page.
      /// </summary>
      void Refresh () {
        Native->refresh();
      }

      /// <summary>
      /// Cancels an active navigation.
      /// </summary>
      void Stop () {
        Native->stop();
      }

      void Cut () {
        Native->cut();
      }

      void Copy () {
        Native->copy();
      }

      void Paste () {
        Native->paste();
      }

      void Undo () {
        Native->undo();
      }

      void Redo () {
        Native->redo();
      }

      void DeleteSelection () {
        Native->del();
      }

      void SelectAll () {
        Native->selectAll();
      }

      /// <summary>
      /// Changes the virtual size of the window.
      /// </summary>
      /// <param name="width">Specifies the new width of the window, in pixels (must be greater than 0).</param>
      /// <param name="height">Specifies the new height of the window, in pixels (must be greater than 0).</param>
      virtual void Resize (int width, int height) {
        Native->resize(width, height);
      }

      /// <summary>
      /// Generates a virtual focus gained event within the window.
      /// </summary>
      void Focus () {
        Focused = true;
        Native->focus();
      }

      /// <summary>
      /// Generates a virtual focus lost event within the window.
      /// </summary>
      void Unfocus () {
        Focused = false;
        Native->unfocus();
      }

    public protected:
      virtual void OnAddressBarChanged (System::String ^ newUrl) {
        AddressBarChanged(this, newUrl);
      }

      virtual void OnStartLoading (System::String ^ newUrl) {
        StartLoading(this, newUrl);
      }

      virtual void OnLoad () {
        Load(this);
      }

      virtual void OnProvisionalLoadError (System::String ^ url, int errorCode, bool isMainFrame) {
        ProvisionalLoadError(this, url, errorCode, isMainFrame);
      }

      virtual void OnCrashed () {
        Crashed(this);
      }

      virtual void OnResponsive () {
        Responsive(this);
      }

      virtual void OnUnresponsive () {
        Unresponsive(this);
      }

      virtual void OnCursorChanged (IntPtr cursorHandle) {
        CursorChanged(this, cursorHandle);
      }

      virtual void OnPaint (IntPtr sourceBuffer, Rect ^ rect, int dx, int dy, Rect ^ scrollRect) {
        Paint(this, sourceBuffer, rect, dx, dy, scrollRect);
      }

      virtual void OnCreatedWindow (Window ^ newWindow, Rect ^ initialRect, System::String ^ creatorUrl) {
        CreatedWindow(this, newWindow, initialRect, creatorUrl);
      }

      virtual void OnChromeSend (System::String ^ message, array<System::String ^> ^ arguments) {
        ChromeSend(this, message, arguments);
      }

      virtual void OnCrashedWorker () {
        CrashedWorker(this);
      }

      virtual void OnCrashedPlugin (System::String ^ pluginName) {
        CrashedPlugin(this, pluginName);
      }

      virtual void OnConsoleMessage (System::String ^ sourceId, System::String ^ message, int lineNumber) {
        ConsoleMessage(this, sourceId, message, lineNumber);
      }

      virtual void OnScriptAlert (System::String ^ message, System::String ^ defaultValue, System::String ^ url, int flags, bool % success, System::String ^% value) {
        ScriptAlert(this, message, defaultValue, url, (ScriptAlertFlags)flags, success, value);
      }

      virtual void OnNavigationRequested (System::String ^ newUrl, System::String ^ referrer, bool isNewWindow, bool &cancelDefaultAction) {
        NavigationRequested(this, newUrl, referrer, isNewWindow, cancelDefaultAction);
      }

      virtual void OnWidgetCreated (Berkelium::Managed::Widget ^ widget, int zIndex) {
        WidgetCreated(this, widget, zIndex);
      }

      virtual void OnWidgetDestroyed (Berkelium::Managed::Widget ^ widget) {
        WidgetDestroyed(this, widget);
        widget->OnDestroyed();
      }

      virtual void OnWidgetPaint (Berkelium::Managed::Widget ^ widget, IntPtr sourceBuffer, Rect ^ rect, int dx, int dy, Rect ^ scrollRect) {
        WidgetPaint(this, widget, sourceBuffer, rect, dx, dy, scrollRect);
        widget->OnPaint(sourceBuffer, rect, dx, dy, scrollRect);
      }

      virtual void OnWidgetMoved (Berkelium::Managed::Widget ^ widget, int newX, int newY) {
        WidgetMoved(this, widget, newX, newY);
        widget->OnMoved(newX, newY);
      }

      virtual void OnWidgetResized (Berkelium::Managed::Widget ^ widget, int newWidth, int newHeight) {
        WidgetResized(this, widget, newWidth, newHeight);
        widget->OnResized(newWidth, newHeight);
      }

      virtual void OnLoadingStateChanged (bool isLoading) {
        LoadingStateChanged(this, isLoading);
      }

      virtual void OnTitleChanged (System::String ^ newTitle) {
        TitleChanged(this, newTitle);
      }

      virtual void OnTooltipChanged (System::String ^ newTooltip) {
        TooltipChanged(this, newTooltip);
      }

      virtual void OnShowContextMenu (ContextMenuEventArgs ^ args) {
        ShowContextMenu(this, args);
      }
    };

  }}
