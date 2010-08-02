// This is the main DLL file.

#include "stdafx.h"

#include "BerkeliumSharp.h"

using namespace System::Text;
using namespace System::Resources;
using namespace System::Reflection;

namespace Berkelium {
  namespace Managed {
    void BerkeliumSharp::Init (String ^ homeDirectory) {
        if (IsInitialized)
          return;

        String ^ dllDirectory;

        if (homeDirectory != nullptr) {
          if (!Directory::Exists(homeDirectory))
            Directory::CreateDirectory(homeDirectory);

          if (!Directory::Exists(homeDirectory))
            throw gcnew ArgumentException(
              "The specified home directory was not found and could not be created."
            );

          dllDirectory = Path::Combine(homeDirectory, "NativeLibraries");
        } else {
          dllDirectory = Path::Combine(
            Environment::GetFolderPath(
              System::Environment::SpecialFolder::LocalApplicationData
            ), "BerkeliumSharp"
          );
        }

        if (!Directory::Exists(dllDirectory))
          Directory::CreateDirectory(dllDirectory);

        Assembly ^ assembly = Assembly::GetExecutingAssembly();
        DateTime fileTime = File::GetLastWriteTimeUtc(assembly->Location);

        array<unsigned char> ^ buffer = gcnew array<unsigned char>(32768);
        for each (String ^ name in assembly->GetManifestResourceNames()) {
          bool shouldExtract = false;

          String ^ outputPath = Path::Combine(dllDirectory, name);

          if (!File::Exists(outputPath))
            shouldExtract = true;
          else if (File::GetLastWriteTimeUtc(outputPath) < fileTime)
            shouldExtract = true;

          if (!shouldExtract)
            continue;

          {
            OutputDebugString(L"Extracting ");
            pin_ptr<const wchar_t> namePtr(PtrToStringChars(name));
            OutputDebugString(namePtr);
            OutputDebugString(L"... ");
          }

          Stream ^ inputStream = assembly->GetManifestResourceStream(name);
          try {
            Stream ^ outputStream = gcnew FileStream(
              outputPath, FileMode::Create, FileAccess::Write, FileShare::None
            );

            outputStream->SetLength(inputStream->Length);
            try {
              while (true) {
                  int readBytes = inputStream->Read(buffer, 0, buffer->Length);
                  if (readBytes <= 0)
                      break;
                  outputStream->Write(buffer, 0, readBytes);
              }
            } finally {
              outputStream->Close();
            }            
          } finally {
            inputStream->Close();
          }

          File::SetCreationTimeUtc(outputPath, fileTime);
          File::SetLastWriteTimeUtc(outputPath, fileTime);

          OutputDebugString(L"done.\r\n");
        }

        {
          pin_ptr<const wchar_t> dllDirPtr(PtrToStringChars(dllDirectory));
          SetDllDirectory(dllDirPtr);
          
          wchar_t pathBuf[4096];
          memset(pathBuf, 0, sizeof(wchar_t) * 4096);
          GetEnvironmentVariable(L"PATH", pathBuf, 4096);
          wcsncat_s(pathBuf, 4096, L";", 1);
          wcsncat_s(pathBuf, 4096, dllDirPtr, dllDirectory->Length);
          wcsncat_s(pathBuf, 4096, L";", 1);
          SetEnvironmentVariable(L"PATH", pathBuf);
        }

        if (homeDirectory != nullptr) {
          pin_ptr<const wchar_t> homeDirPtr(PtrToStringChars(homeDirectory));
          ::Berkelium::init(homeDirPtr, homeDirectory->Length);
        } else {
          ::Berkelium::init(0, 0);
        }

        Wrapper = new ErrorDelegateWrapper();
        ::Berkelium::setErrorHandler(Wrapper);

        IsInitialized = true;
    }

    void GrowBufferForText (Decoder ^ decoder, const char * source, size_t length, wchar_t * &target, size_t &targetSize) {
      int count = decoder->GetCharCount((unsigned char *)source, length, true);

      if (targetSize < count) {
        if (target != 0)
          free(target);

        target = (wchar_t *)malloc(count * sizeof(wchar_t));
        targetSize = count;
      }
    }

    static void CopyToGlobal (array<unsigned char> ^ source, HGLOBAL & target) {
      if (source != nullptr) {
        pin_ptr<unsigned char> ptr = &source[0];
        target = Marshal::AllocHGlobal(source->Length).ToPointer();
        memcpy(target, ptr, source->Length);
      } else
        target = 0;
    }

    ProtocolHandler::ProtocolHandler (
      Managed::Context ^ context, 
      String ^ scheme
    )
      : Scheme(scheme)
      , Context(context)
    {
      Native = new NativeProtocolHandler(this);

      IntPtr schemePtr = Marshal::StringToHGlobalAnsi(scheme);

      context->Native->registerProtocol((const char *)schemePtr.ToPointer(), scheme->Length, Native);

      Marshal::FreeHGlobal(schemePtr);
    }

    ProtocolHandler::~ProtocolHandler () {
      if (Native) {
        IntPtr schemePtr = Marshal::StringToHGlobalAnsi(Scheme);
        Context->Native->unregisterProtocol((const char *)schemePtr.ToPointer(), Scheme->Length);
        Marshal::FreeHGlobal(schemePtr);

        delete Native;
        Native = 0;
        
        Scheme = nullptr;
        Context = nullptr;
      }
    }

    bool NativeProtocolHandler::HandleRequest(const wchar_t * url, size_t urlLength, HGLOBAL &responseBody, HGLOBAL &responseHeaders) {
      return Owner->DoHandleRequest(url, urlLength, responseBody, responseHeaders);
    }

    bool ProtocolHandler::DoHandleRequest(const wchar_t * urlPtr, size_t urlLength, HGLOBAL &responseBody, HGLOBAL &responseHeaders) {
      String ^ url = gcnew String(urlPtr, 0, urlLength);
      array<unsigned char> ^ body = nullptr;
      array<String ^> ^ headers = nullptr;

      bool result = this->HandleRequest(
        url, body, headers
      );

      CopyToGlobal(body, responseBody);

      if (headers == nullptr)
          responseHeaders = 0;
      else {
        {
          int sz = 1;
          for (int i = 0; i < headers->Length; i++)
            sz += headers[i]->Length + 1;
          responseHeaders = Marshal::AllocHGlobal(sz).ToPointer();
          memset(responseHeaders, 0, sz);
        }

        {
          int pos = 0;
          unsigned char * ptr = (unsigned char *)(void *)responseHeaders;
          for (int i = 0; i < headers->Length; i++) {
            int len = headers[i]->Length;
            IntPtr headerPtr = Marshal::StringToHGlobalAnsi(headers[i]);
            memcpy(ptr + pos, headerPtr.ToPointer(), len);
            Marshal::FreeHGlobal(headerPtr);
            pos += len + 1;
          }
        }
      }

      return result;
    }

    Context ^ Context::GetContext(::Berkelium::Context * context, bool ownsHandle) {
      if (!Table)
        Table = new ContextTable();

      ContextTable::iterator iter = Table->find(context);

      if (iter != Table->end())
        return iter->second;

      Context ^ result = gcnew Context(context, ownsHandle);
      Table->operator [](context) = result;

      return result;
    }

    bool Context::ContextDestroyed (::Berkelium::Context * context) {
      ContextTable::iterator iter = Table->find(context);

      if (iter != Table->end()) {
        Table->erase(iter);
        return true;
      }

      return false;
    }

    void ErrorDelegateWrapper::onPureCall() {
      BerkeliumSharp::OnPureCall();
    }

    void ErrorDelegateWrapper::onInvalidParameter(const wchar_t *expression, const wchar_t *function, const wchar_t *file, unsigned int line, uintptr_t reserved) {
      BerkeliumSharp::OnInvalidParameter(
        gcnew String(expression),
        gcnew String(function),
        gcnew String(file),
        line
        );
    }

    void ErrorDelegateWrapper::onOutOfMemory() {
      BerkeliumSharp::OnOutOfMemory();
    }

    void ErrorDelegateWrapper::onAssertion(const char *assertMessage) {
      BerkeliumSharp::OnAssertion(
        gcnew String(assertMessage)
        );
    }

    Widget ^ WindowDelegateWrapper::GetWidget (::Berkelium::Widget * widget, bool ownsHandle) {
      TWidgetTable::iterator iter = WidgetTable.find(widget);

      if (iter != WidgetTable.end())
        return iter->second;

      return (WidgetTable[widget] = gcnew Widget(Owner, widget, ownsHandle));
    }

    bool WindowDelegateWrapper::WidgetDestroyed (::Berkelium::Widget * widget) {
      TWidgetTable::iterator iter = WidgetTable.find(widget);

      if (iter != WidgetTable.end()) {
        WidgetTable.erase(iter);
        return true;
      }

      return false;
    }

    void WindowDelegateWrapper::onCursorUpdated (const Berkelium::Cursor &newCursor) {
      Owner->OnCursorChanged(
        (IntPtr)newCursor.GetCursor()
      );
    }

    void WindowDelegateWrapper::onAddressBarChanged (::Berkelium::Window *win, const wchar_t *newURL, size_t newURLSize) {
      Owner->OnAddressBarChanged(
        gcnew String(newURL, 0, newURLSize)
        );
    }

    void WindowDelegateWrapper::onStartLoading (::Berkelium::Window *win, const wchar_t *newURL, size_t newURLSize) {
      Owner->OnStartLoading(
        gcnew String(newURL, 0, newURLSize)
        );
    }

    void WindowDelegateWrapper::onLoad (::Berkelium::Window *win) {
      Owner->OnLoad();
    }

    void WindowDelegateWrapper::onProvisionalLoadError(::Berkelium::Window *win, const wchar_t * url, size_t urlLength, int errorCode, bool isMainFrame) {
      Owner->OnProvisionalLoadError(
        gcnew String(url, 0, urlLength), errorCode, isMainFrame
        );
    }

    void WindowDelegateWrapper::onCrashed (::Berkelium::Window *win) {
      Owner->OnCrashed();
    }

    void WindowDelegateWrapper::onUnresponsive (::Berkelium::Window *win) {
      Owner->OnUnresponsive();
    }

    void WindowDelegateWrapper::onResponsive (::Berkelium::Window *win) {
      Owner->OnResponsive();
    }

    void WindowDelegateWrapper::onChromeSend (::Berkelium::Window *win, ::Berkelium::WindowDelegate::Data message, const ::Berkelium::WindowDelegate::Data *content, size_t numContents) {
      gcroot<Decoder ^> decoder = Encoding::UTF8->GetDecoder();
      gcroot<array<String ^> ^> _args(
        gcnew array<String ^>(numContents)
        );

      int bytesUsed, charsUsed;
      bool completed;
      wchar_t * buf = 0;
      size_t bufSize = 0;

      for (unsigned int i = 0; i < numContents; i++) {
        const ::Berkelium::WindowDelegate::Data & item = content[i];

        GrowBufferForText(decoder, item.message, item.length, buf, bufSize);
        decoder->Convert((unsigned char *)item.message, item.length, buf, bufSize, true, bytesUsed, charsUsed, completed);

        _args[i] = gcnew String(buf, 0, charsUsed);
      }

      GrowBufferForText(decoder, message.message, message.length, buf, bufSize);
      decoder->Convert((unsigned char *)message.message, message.length, buf, bufSize, true, bytesUsed, charsUsed, completed);

      Owner->OnChromeSend(
        gcnew String(buf, 0, charsUsed),
        _args
      );
    }

    void WindowDelegateWrapper::onCreatedWindow (::Berkelium::Window *win, ::Berkelium::Window *newWindow, ::Berkelium::Rect &initialRect, const wchar_t *url, size_t urlLength) {
      Owner->OnCreatedWindow(
        gcnew Window(Owner->Context, newWindow, true),
        gcnew Rect(initialRect.left(), initialRect.top(), initialRect.width(), initialRect.height()),
        gcnew String(url, 0, urlLength)
      );
    }

    void WindowDelegateWrapper::onPaint (::Berkelium::Window *win, const unsigned char *sourceBuffer, const ::Berkelium::Rect &rect, int dx, int dy, const ::Berkelium::Rect &scrollRect) {
      Owner->OnPaint(
        IntPtr((void *)sourceBuffer),
        gcnew Rect(rect.left(), rect.top(), rect.width(), rect.height()),
        dx, dy,
        gcnew Rect(scrollRect.left(), scrollRect.top(), scrollRect.width(), scrollRect.height())
      );
    }

    void WindowDelegateWrapper::onCrashedWorker(::Berkelium::Window *win) {
      Owner->OnCrashedWorker();
    }

    void WindowDelegateWrapper::onCrashedPlugin(::Berkelium::Window *win, const wchar_t *pluginName, size_t pluginNameLength) {
      Owner->OnCrashedPlugin(
        gcnew String(pluginName, 0, pluginNameLength)
        );
    }

    void WindowDelegateWrapper::onConsoleMessage(::Berkelium::Window *win, const wchar_t *sourceId, size_t sourceIdLength, const wchar_t *message, size_t messageLength, int line_no) {
      Owner->OnConsoleMessage(
        gcnew String(sourceId, 0, sourceIdLength),
        gcnew String(message, 0, messageLength),
        line_no
        );
    }

    void WindowDelegateWrapper::onScriptAlert(::Berkelium::Window *win, const wchar_t *message, size_t messageLength, const wchar_t *defaultValue, size_t defaultValueLength, const wchar_t *url, size_t urlLength, int flags, bool &success, std::wstring &value) {
      String ^ valueStr = nullptr;
      Owner->OnScriptAlert(
        gcnew String(message, 0, messageLength),
        gcnew String(defaultValue, 0, defaultValueLength),
        gcnew String(url, 0, urlLength),
        flags,
        success,
        valueStr
        );

      if (valueStr != nullptr) {
        pin_ptr<const wchar_t> valuePtr = PtrToStringChars(valueStr);
        value = std::wstring(valuePtr, valueStr->Length);
      }
    }

    void WindowDelegateWrapper::onNavigationRequested(::Berkelium::Window *win, const wchar_t *newUrl, size_t newUrlLength, const wchar_t *referrer, size_t referrerLength, bool isNewWindow, bool &cancelDefaultAction) {
      Owner->OnNavigationRequested(
        gcnew String(newUrl, 0, newUrlLength),
        gcnew String(referrer, 0, referrerLength),
        isNewWindow, cancelDefaultAction
        );
    }

    void WindowDelegateWrapper::onWidgetCreated (::Berkelium::Window *win, ::Berkelium::Widget *newWidget, int zIndex) {
      if (newWidget->getId() == win->getId())
        return;

      Owner->OnWidgetCreated(
        GetWidget(newWidget, false), zIndex
      );
    }

    void WindowDelegateWrapper::onWidgetDestroyed (::Berkelium::Window *win, ::Berkelium::Widget *widget) {
      if (widget->getId() == win->getId())
        return;

      Owner->OnWidgetDestroyed(
        GetWidget(widget, false)
      );
      WidgetDestroyed(widget);
    }

    void WindowDelegateWrapper::onWidgetResize (::Berkelium::Window *win, ::Berkelium::Widget *widget, int newWidth, int newHeight) {
      if (widget->getId() == win->getId())
        return;

      Owner->OnWidgetResized(
        GetWidget(widget, false),
        newWidth, newHeight
      );
    }

    void WindowDelegateWrapper::onWidgetMove (::Berkelium::Window *win, ::Berkelium::Widget *widget, int newX, int newY) {
      if (widget->getId() == win->getId())
        return;

      Owner->OnWidgetMoved(
        GetWidget(widget, false),
        newX, newY
      );
    }

    void WindowDelegateWrapper::onWidgetPaint (::Berkelium::Window *win, ::Berkelium::Widget *widget, const unsigned char *sourceBuffer, const ::Berkelium::Rect &rect, int dx, int dy, const ::Berkelium::Rect &scrollRect) {
      if (widget->getId() == win->getId())
        return;

      Owner->OnWidgetPaint(
        GetWidget(widget, false),
        IntPtr((void *)sourceBuffer),
        gcnew Rect(rect.left(), rect.top(), rect.width(), rect.height()),
        dx, dy,
        gcnew Rect(scrollRect.left(), scrollRect.top(), scrollRect.width(), scrollRect.height())
      );
    }

    void WindowDelegateWrapper::onLoadingStateChanged(::Berkelium::Window *win, bool isLoading) {
      Owner->OnLoadingStateChanged(
        isLoading
      );
    }

    void WindowDelegateWrapper::onTitleChanged(::Berkelium::Window *win, const wchar_t *title, size_t titleLength) {
      Owner->OnTitleChanged(
        gcnew String(title, 0, titleLength)
      );
    }

    void WindowDelegateWrapper::onTooltipChanged(::Berkelium::Window *win, const wchar_t *tooltip, size_t tooltipLength) {
      Owner->OnTooltipChanged(
        gcnew String(tooltip, 0, tooltipLength)
      );
    }

    void WindowDelegateWrapper::onShowContextMenu(::Berkelium::Window *win, const ::Berkelium::ContextMenuEventArgs& cargs) {
      ContextMenuEventArgs ^ args = gcnew ContextMenuEventArgs();

      args->MediaType = (MediaType)cargs.mediaType;
      args->MouseX = cargs.mouseX;
      args->MouseY = cargs.mouseY;
      args->LinkUrl = gcnew String(
        cargs.linkUrl, 0, cargs.linkUrlLength
      );
      args->SrcUrl = gcnew String(
        cargs.srcUrl, 0, cargs.srcUrlLength
      );
      args->PageUrl = gcnew String(
        cargs.pageUrl, 0, cargs.pageUrlLength
      );
      args->FrameUrl = gcnew String(
        cargs.frameUrl, 0, cargs.frameUrlLength
      );
      args->SelectedText = gcnew String(
        cargs.selectedText, 0, cargs.selectedTextLength
      );
      args->IsEditable = cargs.isEditable;
      args->EditFlags = (EditFlags)cargs.editFlags;

      Owner->OnShowContextMenu(
        args
      );
    }
  }}