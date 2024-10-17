/*
   Copyright 2024 Ellisnet - Jeremy Ellis (jeremy@ellisnet.com)
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
       http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

//FILE DATE/REVISION: 10/16/2024

// ReSharper disable RedundantCast
// ReSharper disable RedundantAssignment
// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantEmptySwitchSection
// ReSharper disable RedundantAttributeUsageProperty
// ReSharper disable CheckNamespace
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable LocalizableElement
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable ConstantNullCoalescingCondition
// ReSharper disable ConstantConditionalAccessQualifier

#pragma warning disable IDE0079

#pragma warning disable CA1416 // Validate platform compatibility (Win UI)
#pragma warning disable CS1591

//Stop warning about things that shouldn't be able to be null
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8612
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable CS8767

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text;
#if (WIN_UI || HAS_UNO) //WIN_UI needs to be manually defined on Win UI projects (and Uno net8.0-windows projects)
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#elif MAUI //Needs to be manually defined on .NET MAUI projects
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Devices;
#else
using System.Windows;
#endif

#if SIMPLE_ENUM
//Requires C# v11.0 (minimum) - i.e. use with .NET 7 and higher
#endif

#if RESOLVE_SERVICES
//Requires NuGet package: Microsoft.Extensions.Hosting
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#endif

#if USING_WEB_HOST
//Requires NuGet package: Microsoft.AspNetCore.Owin
using Microsoft.AspNetCore.Hosting;
#endif

#if SIMPLE_MESSAGING
//Nothing special needed
#endif

#if (USING_WEB_HOST || SIMPLE_MESSAGING)
using System.Threading;
#endif

#if SIMPLE_HTTP_CLIENT
//Requires NuGet package: Microsoft.Extensions.Http
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using IO = System.IO;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory; //Added this line so the problem of missing NuGet package shows up at the top
#endif

//If this code file is being used in a console app, we probably just want to disable all the UI portions
#if !DISABLE_UI

// ReSharper disable InconsistentNaming

public enum SimpleDialogButtons
{
    OK = 0,
    OKCancel = 1,
    YesNo = 2
}

public enum SimpleDialogResult
{
    None = 0,
    OK = 1,
    Cancel = 2,
    Yes = 3,
    No = 4
}

// ReSharper restore InconsistentNaming

public class SimpleDialog : IDisposable
{
    private bool _isDisposed;

    private string _message = "";
    public string Message
    {
        get => _message;
        set => _message = (value ?? "").Trim();
    }

    private string _title;
    public string Title
    {
        get => _title;
        set => _title = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public SimpleDialogButtons Buttons { get; set; }

#if (WIN_UI || HAS_UNO)
    private SimpleDialog(
        Func<XamlRoot> xamlRootGetter,
        DispatcherQueue dispatcher,
        string message, 
        string title, 
        SimpleDialogButtons buttons)
    {
        _xamlRootGetter = xamlRootGetter ?? throw new ArgumentNullException(nameof(xamlRootGetter));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Message = message;
        Title = title;
        Buttons = buttons;
    }

    public static SimpleDialog Create(
        Func<XamlRoot> xamlRootGetter,
        DispatcherQueue dispatcher,
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) => 
        new(xamlRootGetter, dispatcher, message, title, buttons);

    private string BreakOnMaxLineLength(string text, int maxLineLength)
    {
        var result = text;

        if ((!string.IsNullOrWhiteSpace(text)) && maxLineLength > 0)
        {
            var lines = text.Replace("\r\n", "\n").Trim().Split('\n');
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.Length > maxLineLength)
                {
                    var pos = 0;
                    while (pos < line.Length)
                    {
                        var newLine = ((pos + maxLineLength) < line.Length)
                            ? line.Substring(pos, maxLineLength)
                            : line.Substring(pos);
                        sb.AppendLine(newLine);
                        pos += newLine.Length;
                    }
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            result = sb.ToString().Trim();
        }

        return result;
    }

    // ReSharper disable InconsistentNaming
    //VERY IMPORTANT: With Uno, anything that touches the XamlRoot needs to be running on the main thread.
    protected Func<XamlRoot> _xamlRootGetter;
    protected DispatcherQueue _dispatcher;
    // ReSharper restore InconsistentNaming
#elif MAUI
    private SimpleDialog(
        Func<Page> xamlRootGetter,
        string message,
        string title,
        SimpleDialogButtons buttons)
    {
        _xamlRootGetter = xamlRootGetter ?? throw new ArgumentNullException(nameof(xamlRootGetter));
        Message = message;
        Title = title;
        Buttons = buttons;
    }

    public static SimpleDialog Create(
        Func<Page> xamlRootGetter,
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        new(xamlRootGetter, message, title, buttons);

    // ReSharper disable InconsistentNaming
    protected Func<Page> _xamlRootGetter;
    // ReSharper restore InconsistentNaming
#else
    private SimpleDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK)
    {
        Message = message;
        Title = title;
        Buttons = buttons;
    }

    public static SimpleDialog Create(string message, string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        new(message, title, buttons);
#endif

    public async Task<SimpleDialogResult> ShowAsync()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException("Dialog has been disposed.");
        }

        var result = SimpleDialogResult.None;

#if (WIN_UI || HAS_UNO || MAUI)
        string firstButton;
        SimpleDialogResult firstButtonResult;
        string secondButton = null;
        SimpleDialogResult secondButtonResult = SimpleDialogResult.None;

        switch (this.Buttons)
        {
            case SimpleDialogButtons.OK:
                firstButton = "OK";
                firstButtonResult = SimpleDialogResult.OK;
                break;
            case SimpleDialogButtons.OKCancel:
                firstButton = "OK";
                firstButtonResult = SimpleDialogResult.OK;
                secondButton = "Cancel";
                secondButtonResult = SimpleDialogResult.Cancel;
                break;
            case SimpleDialogButtons.YesNo:
                firstButton = "Yes";
                firstButtonResult = SimpleDialogResult.Yes;
                secondButton = "No";
                secondButtonResult = SimpleDialogResult.No;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
#endif

#if (WIN_UI || HAS_UNO)
        result = await _dispatcher.InvokeOnMainThreadAsync(async () =>
        {
            var dlgResult = SimpleDialogResult.None;

            var dialog = new ContentDialog
            {
                Content = new TextBlock { Text = BreakOnMaxLineLength(_message, 74) },
                PrimaryButtonText = firstButton,
                IsPrimaryButtonEnabled = true,
                IsSecondaryButtonEnabled = (secondButton != null),
                XamlRoot = _xamlRootGetter.Invoke()
            };
            if (secondButton != null)
            {
                dialog.SecondaryButtonText = secondButton;
            }
            if (_title != null)
            {
                dialog.Title = _title;
            }

            var dialogResult = await dialog.ShowAsync(ContentDialogPlacement.Popup);
            if (dialogResult == ContentDialogResult.Primary)
            {
                dlgResult = firstButtonResult;
            }
            else if (dialogResult == ContentDialogResult.Secondary)
            {
                dlgResult = secondButtonResult;
            }

            return dlgResult;
        });
#elif MAUI
        if (secondButton == null)
        {
            await MainThreadHelper.SafeInvokeOnMainThreadAsync(async () =>
            {
                await _xamlRootGetter.Invoke().DisplayAlert((_title ?? ""), _message, firstButton);
            });
            result = firstButtonResult;
        }
        else
        {
            result = await MainThreadHelper.SafeInvokeOnMainThreadAsync(async () => 
                (await _xamlRootGetter.Invoke().DisplayAlert((_title ?? ""), _message, firstButton, secondButton)) 
                    ? firstButtonResult 
                    : secondButtonResult);
        }
#else

        MessageBoxButton msgButton;
        switch (Buttons)
        {
            case SimpleDialogButtons.OK:
                msgButton = MessageBoxButton.OK;
                break;
            case SimpleDialogButtons.OKCancel:
                msgButton = MessageBoxButton.OKCancel;
                break;
            case SimpleDialogButtons.YesNo:
                msgButton = MessageBoxButton.YesNo;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        MessageBoxResult dialogResult = MessageBox.Show(_message, (_title ?? ""), msgButton);

        switch (dialogResult)
        {
            case MessageBoxResult.OK:
                result = SimpleDialogResult.OK;
                break;

            case MessageBoxResult.Cancel:
                result = SimpleDialogResult.Cancel;
                break;

            case MessageBoxResult.Yes:
                result = SimpleDialogResult.Yes;
                break;

            case MessageBoxResult.No:
                result = SimpleDialogResult.No;
                break;

            default:
                break;
        }

        //satisfy the compiler that something async is happening
        await Task.Run(() => { });

#endif

        return result;
    }

    #region | IDisposable implementation |

    public void Dispose()
    {
        _isDisposed = true;
#if (WIN_UI || HAS_UNO)
        _xamlRootGetter = null;
        _dispatcher = null;
#elif (MAUI)
        _xamlRootGetter = null;
#endif
    }

    #endregion
}

#if (WIN_UI || HAS_UNO)
internal static class DispatcherHelper
{
    internal static void InvokeOnMainThread(this DispatcherQueue dispatcher, Action functionToExecute)
    {
        if (dispatcher == null) { throw new ArgumentNullException(nameof(dispatcher));}

        if (functionToExecute != null)
        {
            dispatcher.TryEnqueue(functionToExecute.Invoke);
        }
    }

    internal static Task<T> InvokeOnMainThreadAsync<T>(this DispatcherQueue dispatcher, Func<Task<T>> functionToExecute)
    {
        if (dispatcher == null) { throw new ArgumentNullException(nameof(dispatcher)); }
        if (functionToExecute == null) { throw new ArgumentNullException(nameof(functionToExecute)); }

        var completionSource = new TaskCompletionSource<T>();

        // ReSharper disable once AsyncVoidLambda
        dispatcher.TryEnqueue(async () =>
        {
            try
            {
                T result = await functionToExecute.Invoke();
                completionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }
        });

        return completionSource.Task;
    }
}

public interface IXamlRootGetter
{
    public void SetXamlRootGetter(Func<XamlRoot> getter);
}
#endif

#if MAUI
internal static class MainThreadHelper
{
    internal static void SafeInvokeOnMainThread(Action functionToExecute)
    {
        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                functionToExecute.Invoke();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(functionToExecute.Invoke);
            }
        }
    }

    internal static async Task SafeInvokeOnMainThreadAsync(Func<Task> functionToExecute)
    {
        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                await functionToExecute.Invoke();
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(functionToExecute);
            }
        }
    }

    internal static async Task<T> SafeInvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute)
    {
        T result = default;

        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                result = await functionToExecute.Invoke();
            }
            else
            {
                result = await MainThread.InvokeOnMainThreadAsync(functionToExecute);
            }
        }

        return result;
    }
}

public interface IXamlRootGetter
{
    public void SetXamlRootGetter(Func<Page> getter);
}
#endif

#if (WIN_UI || HAS_UNO || MAUI)
public abstract class SimpleViewModel : IXamlRootGetter, INotifyPropertyChanged, IDisposable
#else
public abstract class SimpleViewModel : INotifyPropertyChanged, IDisposable
#endif
{
    // ReSharper disable once InconsistentNaming
    private static bool? _isInDesignMode;

#if (WIN_UI || HAS_UNO || MAUI)
    //Don't currently know how to check and see if the view-model instance is in "design mode" -
    //  for WinUI and .NET MAUI.
    protected bool IsDesignMode(bool defaultValueIfNotSet) =>
        _isInDesignMode ?? defaultValueIfNotSet;
#else
    protected bool IsDesignMode(bool? defaultValueIfNotSet = null)
    {
        if (!_isInDesignMode.HasValue)
        {
            if (defaultValueIfNotSet.HasValue)
            {
                return defaultValueIfNotSet.Value;
            }

            //Checking GetIsInDesignMode() works fine (WPF-only so far) when the application is being designed
            //  in Visual Studio; but does not work correctly when being designed in JetBrains Rider.
            _isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }
        return _isInDesignMode.Value;
    }
#endif

    public static void SetIsDesignMode(bool isDesignMode) => _isInDesignMode = isDesignMode;

#if (WIN_UI || HAS_UNO)
    private DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private Func<XamlRoot> _xamlRootGetter;

    protected XamlRoot GetXamlRoot()
    {
        try
        {
            if (_xamlRootGetter == null)
            {
                throw new InvalidOperationException(
                    $"Unable to perform the requested UI operation before {nameof(SetXamlRootGetter)}() has been called.");
            }

            return _xamlRootGetter.Invoke();
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception e)
        {
            throw new InvalidOperationException("Getting XamlRoot on WinUI and Uno can fail if not executed on the main thread.", e);
        }
    }

    //Example of how this SetXamlRootGetter() method should be used in the constructor of a
    //  Microsoft.UI.Xaml (Win UI) code-behind file:

    //1) The root element in the XAML file should have a name - since the DataContext is set at
    //  the root element level (not at the Window level) in Win UI - like this:
    //  <Grid x:Name="RootUiElement">
    //    <Grid.DataContext>
    //      <vm:MainViewModel />
    //    </Grid.DataContext>
    //    [...rest of page...]
    //  </Grid>

    //2) The constructor of the code-behind file for the XAML window should have this
    //  (after the InitializeComponent() call):
    //    (this.RootUiElement.DataContext as IXamlRootGetter)?
    //      .SetXamlRootGetter(() => this.RootUiElement.XamlRoot);

    public void SetXamlRootGetter(Func<XamlRoot> getter) => _xamlRootGetter = getter 
        ?? throw new ArgumentNullException(nameof(getter));
#endif

#if MAUI
    private Func<Page> _xamlRootGetter;

    protected Page GetXamlRoot()
    {
        if (_xamlRootGetter == null)
        {
            throw new InvalidOperationException(
                $"Unable to perform the requested UI operation before {nameof(SetXamlRootGetter)}() has been called.");
        }

        return _xamlRootGetter.Invoke();
    }

    //Example of how this SetXamlRootGetter() method should be called from the code-behind file
    //  of a .NET MAUI XAML Page (e.g. ContentPage):

    //1) The Page should have the BindingContext set to an instance of the viewmodel, like this:
    //  (with the 'vm' namespace set as: xmlns:vm="clr-namespace:MyApplication.ViewModels" )
    //  <ContentPage.BindingContext>
    //    <vm:MainViewModel />
    //  </ContentPage.BindingContext>

    //2) Override OnBindingContextChanged() in the code-behind file for the Page as:
    //  protected override void OnBindingContextChanged()
    //  {
    //    base.OnBindingContextChanged();
    //    (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this);
    //  }

    public void SetXamlRootGetter(Func<Page> getter) => _xamlRootGetter = getter
        ?? throw new ArgumentNullException(nameof(getter));
#endif

#if RESOLVE_SERVICES
    protected T GetService<T>() where T : class => SimpleServiceResolver.Instance.GetService<T>();
    protected IEnumerable<T> GetServices<T>() where T : class => SimpleServiceResolver.Instance.GetServices<T>();
#endif

#if SIMPLE_MESSAGING

#if RESOLVE_SERVICES

    protected void MessagingSend<TSender, TArgs>(TSender sender, string message, TArgs args)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Send(sender, message, args);

    protected void MessagingSend<TSender>(TSender sender, string message)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Send(sender, message);

    protected void MessagingSubscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Action<TSender, TArgs> callback,
        TSender source)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback, source);

    protected void MessagingSubscribeFrom<TSender>(
        object subscriber,
        string message,
        Action<TSender> callback,
        TSender source) where TSender : class =>
        (GetService<ISimpleMessaging>()).SubscribeFrom(subscriber, message, callback, source);

    protected void MessagingSubscribe<TArgs>(
        object subscriber,
        string message,
        Action<TArgs> callback) =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback);

    protected void MessagingSubscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Func<TSender, TArgs, Task> callback,
        TSender source)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback, source);

    protected void MessagingSubscribeFrom<TSender>(
        object subscriber,
        string message,
        Func<TSender, Task> callback,
        TSender source) where TSender : class =>
        (GetService<ISimpleMessaging>()).SubscribeFrom(subscriber, message, callback, source);

    protected void MessagingSubscribe<TArgs>(
        object subscriber,
        string message,
        Func<TArgs, Task> callback) =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback);

    protected void MessagingUnsubscribe<TSender, TArgs>(object subscriber, string message)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Unsubscribe<TSender, TArgs>(subscriber, message);

    protected void MessagingUnsubscribeFrom<TSender>(object subscriber, string message)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).UnsubscribeFrom<TSender>(subscriber, message);

    protected void MessagingUnsubscribe<TArgs>(object subscriber, string message) =>
        (GetService<ISimpleMessaging>()).Unsubscribe<TArgs>(subscriber, message);

#else

    protected void MessagingSend<TSender, TArgs>(TSender sender, string message, TArgs args) 
        where TSender : class =>
        SimpleMessaging.Send(sender, message, args);
    
    protected void MessagingSend<TSender>(TSender sender, string message) 
        where TSender : class =>
        SimpleMessaging.Send(sender, message);
    
    protected void MessagingSubscribe<TSender, TArgs>(
        object subscriber, 
        string message, 
        Action<TSender, TArgs> callback, 
        TSender source) 
        where TSender : class => 
        SimpleMessaging.Subscribe(subscriber, message, callback, source);
    
    protected void MessagingSubscribeFrom<TSender>(
        object subscriber, 
        string message, 
        Action<TSender> callback, 
        TSender source) where TSender : class => 
        SimpleMessaging.SubscribeFrom(subscriber, message, callback, source);
    
    protected void MessagingSubscribe<TArgs>(
        object subscriber,
        string message,
        Action<TArgs> callback) =>
        SimpleMessaging.Subscribe(subscriber, message, callback);

    protected void MessagingSubscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Func<TSender, TArgs, Task> callback,
        TSender source)
        where TSender : class =>
        SimpleMessaging.Subscribe(subscriber, message, callback, source);

    protected void MessagingSubscribeFrom<TSender>(
        object subscriber,
        string message,
        Func<TSender, Task> callback,
        TSender source) where TSender : class =>
        SimpleMessaging.SubscribeFrom(subscriber, message, callback, source);

    protected void MessagingSubscribe<TArgs>(
        object subscriber,
        string message,
        Func<TArgs, Task> callback) =>
        SimpleMessaging.Subscribe(subscriber, message, callback);

    protected void MessagingUnsubscribe<TSender, TArgs>(object subscriber, string message) 
        where TSender : class =>
        SimpleMessaging.Unsubscribe<TSender, TArgs>(subscriber, message);
    
    protected void MessagingUnsubscribeFrom<TSender>(object subscriber, string message) 
        where TSender : class =>
        SimpleMessaging.UnsubscribeFrom<TSender>(subscriber, message);

    protected void MessagingUnsubscribe<TArgs>(object subscriber, string message) =>
        SimpleMessaging.Unsubscribe<TArgs>(subscriber, message);

#endif

#endif

    #region | Dialog helpers |

#if (WIN_UI || HAS_UNO)
    protected virtual SimpleDialog CreateDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        SimpleDialog.Create(_xamlRootGetter, _dispatcher, message, title, buttons);
#elif (MAUI)
    protected virtual SimpleDialog CreateDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        SimpleDialog.Create(_xamlRootGetter, message, title, buttons);
#else
    protected virtual SimpleDialog CreateDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        SimpleDialog.Create(message, title, buttons);
#endif

    protected virtual async Task ShowInfo(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            using var dialog = CreateDialog(message, "Information");
            _ = await dialog.ShowAsync();
        }
    }

    protected virtual async Task ShowError(string message, string details = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = $"An error occurred:\n   {message.Trim()}";
            details = (string.IsNullOrWhiteSpace(details))
                ? ""
                : (details.Trim().Length > 200)
                    ? $"{details.Trim().Substring(0, 195).Trim()}[...]"
                    : details.Trim();
            message += (details == "")
                ? ""
                : $"\n\nDetails:\n{details}";
            using var dialog = CreateDialog(message, "ERROR");
            _ = await dialog.ShowAsync();
        }
    }

    protected virtual async Task ShowError(Exception exception, string message = null)
    {
        if (exception != null)
        {
            var msg = (string.IsNullOrWhiteSpace(message))
                ? exception.Message
                : $"{message.Trim()} - {exception.Message}";
            await ShowError(msg, exception.ToString());
        }
    }

    protected virtual async Task<bool> ConfirmDialog(
        string message,
        string title = "Are you sure?",
        SimpleDialogButtons confirmButtons = SimpleDialogButtons.YesNo)
    {
        title = (string.IsNullOrWhiteSpace(title))
            ? "Are you sure?"
            : title.Trim();

        message = (string.IsNullOrWhiteSpace(message))
            ? title
            : message.Trim();

        var confirmResult = (confirmButtons == SimpleDialogButtons.YesNo)
            ? SimpleDialogResult.Yes
            : SimpleDialogResult.OK;

        using var confirm = CreateDialog(message, title, confirmButtons);
        return (await confirm.ShowAsync()) == confirmResult;
    }

    #endregion

#if (WIN_UI || HAS_UNO)

    protected Visibility GetVisibility(bool isVisible) {
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    protected virtual void InvokeOnMainThread(Action functionToExecute) =>
        _dispatcher.InvokeOnMainThread(functionToExecute);

    protected virtual Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute) =>
        _dispatcher.InvokeOnMainThreadAsync(functionToExecute);

#elif MAUI

    protected Visibility GetVisibility(bool isVisible)
    {
        return isVisible ? Visibility.Visible : Visibility.Hidden;
    }

    protected virtual void InvokeOnMainThread(Action functionToExecute)
    {
        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                functionToExecute.Invoke();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(functionToExecute.Invoke);
            }
        }
    }

    protected virtual async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute)
    {
        T result = default;

        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                result = await functionToExecute.Invoke();
            }
            else
            {
                result = await MainThread.InvokeOnMainThreadAsync(functionToExecute);
            }
        }

        return result;
    }

#else

    protected Visibility GetVisibility(bool isVisible)
    {
        return isVisible ? Visibility.Visible : Visibility.Hidden;
    }

    protected virtual void InvokeOnMainThread(Action functionToExecute)
    {
        if (functionToExecute != null)
        {
            // ReSharper disable once PossibleNullReferenceException
            Application.Current.Dispatcher.Invoke(functionToExecute.Invoke);
        }
    }

    protected virtual Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute)
    {
        var completionSource = new TaskCompletionSource<T>();

        // ReSharper disable once PossibleNullReferenceException
        Application.Current.Dispatcher.Invoke(async () =>
        {
            try
            {
                T result = await functionToExecute.Invoke();
                completionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }
        });

        return completionSource.Task;
    }

#endif

    #region | INotifyPropertyChanged implementation and helper methods |

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void NotifyPropertyChanged(string propertyName)
    {
        if ((!string.IsNullOrWhiteSpace(propertyName)))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            CheckAffectedProperties(propertyName);
            CheckAffectedCommands(propertyName);
        }
    }

    protected virtual void ThisPropertyChanged([CallerMemberName] string propertyName = "") =>
        NotifyPropertyChanged(propertyName);

    protected virtual void CheckAffectedProperties(string propertyName)
    {
        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            var propInfo = GetType().GetProperties()
                .FirstOrDefault(f => f.Name.Equals(propertyName.Trim(),
                    StringComparison.InvariantCultureIgnoreCase));
            if (propInfo != null)
            {
                var attrib = propInfo
                    .GetCustomAttributes<AffectsPropertiesAttribute>(true)
                    .FirstOrDefault();

                if (attrib != null)
                {
                    foreach (var affected in attrib.AffectedProperties)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(affected));
                    }
                }
            }
        }
    }

    protected virtual void CheckAffectedCommands(string propertyName)
    {
        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            var propInfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var propInfo = propInfos.FirstOrDefault(f => f.Name.Equals(propertyName.Trim(),
                StringComparison.InvariantCultureIgnoreCase));
            if (propInfo != null)
            {
                //1 - Check for AffectsAllCommandsAttribute
                var affectsAll = propInfo
                    .GetCustomAttributes<AffectsAllCommandsAttribute>(true)
                    .FirstOrDefault();

                if (affectsAll != null)
                {
                    foreach (var cmdPropInfo in propInfos.Where(w => w.PropertyType == typeof(SimpleCommand)))
                    {
                        (cmdPropInfo.GetValue(this) as SimpleCommand)?.RaiseCanExecuteChanged();
                    }
                }
                else
                {
                    //2 - Check for AffectsCommandsAttribute
                    var affectsCmds = propInfo
                        .GetCustomAttributes<AffectsCommandsAttribute>(true)
                        .FirstOrDefault();

                    if (affectsCmds != null)
                    {
                        foreach (var affected in affectsCmds.AffectedCommands)
                        {
                            var cmdPropInfo = propInfos.FirstOrDefault(f => f.Name.Equals(affected,
                                StringComparison.InvariantCultureIgnoreCase));
                            if (cmdPropInfo != null)
                            {
                                (cmdPropInfo.GetValue(this) as SimpleCommand)?.RaiseCanExecuteChanged();
                            }
                        }
                    }
                }
            }
        }
    }

    protected virtual void SetProperty<T>(ref T property, T newValue, [CallerMemberName] string propertyName = "")
        where T : class
    {
        if ((property == null && newValue != null)
            || (property != null && (newValue == null || (!property.Equals(newValue)))))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName);
        }
    }

    protected virtual void SetEnumProperty<TEnum>(ref TEnum property, TEnum newValue, [CallerMemberName] string propertyName = "")
        where TEnum : Enum
    {
        if (Enum.IsDefined(typeof(TEnum), property)
            && Enum.IsDefined(typeof(TEnum), newValue)
            && (!property.Equals(newValue)))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName);
        }
    }

    protected virtual void SetProperty(ref string property, string newValue, [CallerMemberName] string propertyName = "")
    {
        if ((property == null && newValue != null)
            || (property != null && (newValue == null || (!property.Equals(newValue)))))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName);
        }
    }

    protected virtual void SetProperty(ref bool property, bool newValue, [CallerMemberName] string propertyName = "")
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName);
        }
    }

    protected virtual void SetProperty(ref DateTime property, DateTime newValue, [CallerMemberName] string propertyName = "")
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName);
        }
    }

    protected virtual void SetProperty(ref DateTimeOffset property, DateTimeOffset newValue, [CallerMemberName] string propertyName = "")
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName);
        }
    }

    protected virtual void SetProperty(ref int property, int newValue, [CallerMemberName] string propertyName = "")
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName);
        }
    }

    #endregion

    #region | IDisposable implementation |

    public virtual void Dispose()
    {
        // remove event handlers before setting event to null
        Delegate[] delegates = PropertyChanged?.GetInvocationList();
        if (delegates != null)
        {
            foreach (var d in delegates)
            {
                PropertyChanged -= (PropertyChangedEventHandler)d;
            }
        }
        PropertyChanged = null;

#if (WIN_UI || HAS_UNO)
        _dispatcher = null;
#endif

#if (WIN_UI || HAS_UNO || MAUI)
        _xamlRootGetter = null;
#endif
    }

    #endregion
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AffectsPropertiesAttribute : Attribute
{
    public IList<string> AffectedProperties { get; }

    public AffectsPropertiesAttribute(params string[] propertyNames) =>
        AffectedProperties = (propertyNames ?? Array.Empty<string>())
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(s => s.Trim())
            .ToArray();
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AffectsCommandsAttribute : Attribute
{
    public IList<string> AffectedCommands { get; }

    public AffectsCommandsAttribute(params string[] commandNames) =>
        AffectedCommands = (commandNames ?? Array.Empty<string>())
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(s => s.Trim())
            .ToArray();
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AffectsAllCommandsAttribute : Attribute;

public class SimpleCommand : ICommand, IDisposable
{
#if (WIN_UI || HAS_UNO)
    private DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
#endif

    private enum ExecuteStyle
    {
        None = 0,
        SyncWithParam,
        SyncNoParam,
        AsyncWithParam,
        AsyncNoParam,
    }

    private readonly ExecuteStyle _executeStyle;

    //Sync execution styles
    private Action<object> _executeWithParamSync; //allows passing an object parameter to function
    private Action _executeNoParamSync; //no parameter passing

    //Async execution styles
    private Func<object, Task> _executeWithParamAsync; //allows passing an object parameter to function
    private Func<Task> _executeNoParamAsync; //no parameter passing

    private Func<object, bool> _canExecuteWithParam; //allows passing an object parameter to function
    private Func<bool> _canExecuteNoParam; //no parameter passing
    private readonly bool _executeOnMainThread;

    private SimpleCommand(
        Func<object, bool> canExecuteWithParam, Action<object> executeWithParamSync, Func<object, Task> executeWithParamAsync,
        Func<bool> canExecuteNoParam, Action executeNoParamSync, Func<Task> executeNoParamAsync,
        bool executeOnMainThread,
        ExecuteStyle executeStyle)
    {
        _canExecuteWithParam = canExecuteWithParam;
        _executeWithParamSync = executeWithParamSync;
        _executeWithParamAsync = executeWithParamAsync;
        _canExecuteNoParam = canExecuteNoParam;
        _executeNoParamSync = executeNoParamSync;
        _executeNoParamAsync = executeNoParamAsync;
        _executeOnMainThread = executeOnMainThread;
        _executeStyle = executeStyle;
    }

    private bool IsExecutable => Enum.IsDefined(_executeStyle)
                                   && (_executeStyle != ExecuteStyle.None)
                                   && ((_executeStyle == ExecuteStyle.SyncWithParam && _executeWithParamSync != null)
                                       || (_executeStyle == ExecuteStyle.AsyncWithParam && _executeWithParamAsync != null)
                                       || (_executeStyle == ExecuteStyle.SyncNoParam && _executeNoParamSync != null)
                                       || (_executeStyle == ExecuteStyle.AsyncNoParam && _executeNoParamAsync != null));

    #region | SyncWithParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Action<object> executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, executeFunction,
            null, null, null, null,
            executeOnMainThread, ExecuteStyle.SyncWithParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Action<object> executeFunction,
        bool executeOnMainThread = false)
        : this(null, executeFunction, null,
            canExecuteFunction, null, null,
            executeOnMainThread, ExecuteStyle.SyncWithParam)
    { }

    public SimpleCommand(Action<object> executeFunction, bool executeOnMainThread = false)
        : this(null, executeFunction, null,
            null, null, null,
            executeOnMainThread, ExecuteStyle.SyncWithParam)
    { }

    #endregion

    #region | SyncNoParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Action executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, null, null,
            null, executeFunction, null,
            executeOnMainThread, ExecuteStyle.SyncNoParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Action executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            canExecuteFunction, executeFunction, null,
            executeOnMainThread, ExecuteStyle.SyncNoParam)
    { }

    public SimpleCommand(Action executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            null, executeFunction, null,
            executeOnMainThread, ExecuteStyle.SyncNoParam)
    { }

    #endregion

    #region | AsyncWithParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Func<object, Task> executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, null, executeFunction,
            null, null, null,
            executeOnMainThread, ExecuteStyle.AsyncWithParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Func<object, Task> executeFunction,
        bool executeOnMainThread = false)
        : this(null, null, executeFunction,
            canExecuteFunction, null, null,
            executeOnMainThread, ExecuteStyle.AsyncWithParam)
    { }

    public SimpleCommand(Func<object, Task> executeFunction, bool executeOnMainThread = false)
        : this(null, null, executeFunction,
            null, null, null,
            executeOnMainThread, ExecuteStyle.AsyncWithParam)
    { }

    #endregion

    #region | AsyncNoParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Func<Task> executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, null, null,
            null, null, executeFunction,
            executeOnMainThread, ExecuteStyle.AsyncNoParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Func<Task> executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            canExecuteFunction, null, executeFunction,
            executeOnMainThread, ExecuteStyle.AsyncNoParam)
    { }

    public SimpleCommand(Func<Task> executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            null, null, executeFunction,
            executeOnMainThread, ExecuteStyle.AsyncNoParam)
    { }

    #endregion

    public bool CanExecute(object parameter)
    {
        var result = false;

        if (IsExecutable && _canExecuteWithParam != null)
        {
            result = _canExecuteWithParam.Invoke(parameter);
        }
        else if (IsExecutable && _canExecuteNoParam != null)
        {
            result = _canExecuteNoParam.Invoke();
        }

        return result;
    }

    private async void WaitForExecute(TaskCompletionSource tsc, Func<object, Task> execute, object parameter)
    {
        if (tsc == null) { throw new ArgumentNullException(nameof(tsc)); }
        if (execute == null) { throw new ArgumentNullException(nameof(execute)); }

        await execute.Invoke(parameter);
        tsc.SetResult();
    }

    private async void WaitForExecute(TaskCompletionSource tsc, Func<Task> execute)
    {
        if (tsc == null) { throw new ArgumentNullException(nameof(tsc)); }
        if (execute == null) { throw new ArgumentNullException(nameof(execute)); }

        await execute.Invoke();
        tsc.SetResult();
    }

    public async void Execute(object parameter)
    {
        if (IsExecutable)
        {
            var executeOnMain = _executeOnMainThread;

#if MAUI
            //Check to see if we are already on the main thread
            executeOnMain = executeOnMain && (!MainThread.IsMainThread);
#endif

            switch (_executeStyle)
            {
                case ExecuteStyle.SyncWithParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_UNO)
                        _dispatcher.TryEnqueue(() => { _executeWithParamSync.Invoke(parameter); });
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(() => { _executeWithParamSync.Invoke(parameter); });
#else
                        Application.Current.Dispatcher.Invoke(() => { _executeWithParamSync.Invoke(parameter); });
#endif
                    }
                    else
                    {
                        _executeWithParamSync.Invoke(parameter);
                    }
                    break;

                case ExecuteStyle.SyncNoParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_UNO)
                        _dispatcher.TryEnqueue(_executeNoParamSync.Invoke);
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(() => { _executeNoParamSync.Invoke(); });
#else
                        Application.Current.Dispatcher.Invoke(_executeNoParamSync.Invoke);
#endif
                    }
                    else
                    {
                        _executeNoParamSync.Invoke();
                    }
                    break;

                case ExecuteStyle.AsyncWithParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_UNO)
                        var tsc = new TaskCompletionSource();
                        var queued = _dispatcher.TryEnqueue(() => { WaitForExecute(tsc, _executeWithParamAsync, parameter); });
                        if (queued)
                        {
                            await tsc.Task;
                        }
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(async () => { await _executeWithParamAsync.Invoke(parameter); });
#else
                        await Application.Current.Dispatcher.Invoke(async () => { await _executeWithParamAsync.Invoke(parameter); });
#endif
                    }
                    else
                    {
                        await _executeWithParamAsync.Invoke(parameter);
                    }
                    break;

                case ExecuteStyle.AsyncNoParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_UNO)
                        var tsc = new TaskCompletionSource();
                        var queued = _dispatcher.TryEnqueue(() => { WaitForExecute(tsc, _executeNoParamAsync); });
                        if (queued)
                        {
                            await tsc.Task;
                        }
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(async () => { await _executeNoParamAsync.Invoke(); });
#else
                        await Application.Current.Dispatcher.Invoke(async () => { await _executeNoParamAsync.Invoke(); });
#endif
                    }
                    else
                    {
                        await _executeNoParamAsync.Invoke();
                    }
                    break;
            }
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler CanExecuteChanged;

    public void Dispose()
    {
        // remove event handlers before setting event to null
        Delegate[] delegates = CanExecuteChanged?.GetInvocationList();
        if (delegates != null)
        {
            foreach (var d in delegates)
            {
                CanExecuteChanged -= (EventHandler)d;
            }
        }
        CanExecuteChanged = null;

        _canExecuteWithParam = null;
        _executeWithParamSync = null;
        _executeWithParamAsync = null;
        _canExecuteNoParam = null;
        _executeNoParamSync = null;
        _executeNoParamAsync = null;

#if (WIN_UI || HAS_UNO)
        _dispatcher = null;
#endif
    }
}

#endif

#if SIMPLE_ENUM

public interface ISimpleEnumInfo
{
    string Description { get; }
    Type EnumType { get; }
}

public abstract class SimpleEnumInfo<TEnum> : ISimpleEnumInfo
    where TEnum : Enum
{
    public TEnum Member { get; }

    protected SimpleEnumInfo(TEnum member)
    {
        if (!Enum.IsDefined(typeof(TEnum), member))
        {
            throw new ArgumentOutOfRangeException(nameof(member),
                $"Not a valid member of {typeof(TEnum).Name}");
        }
        Member = member;
    }

    protected static TInfo FindInfo<TInfo>(TEnum member)
        where TInfo : class, ISimpleEnumInfo =>
        SimpleEnumHelper.FindMemberInfo<TEnum, TInfo>(member);

    protected static Dictionary<TEnum, TInfo> GetDictionary<TInfo>()
        where TInfo : class, ISimpleEnumInfo =>
        SimpleEnumHelper.GetInfoDictionary<TEnum, TInfo>();

    #region | ISimpleEnumInfo implementation |

    public string Description { get; protected set; }
    public Type EnumType => typeof(TEnum);

    #endregion
}

public interface ISimpleEnumInfoAttribute
{
    Type InfoType { get; }
    string InfoMemberName { get; }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class SimpleEnumAttribute<TInfo> : Attribute, ISimpleEnumInfoAttribute
    where TInfo : class, ISimpleEnumInfo
{
    public SimpleEnumAttribute(string infoMemberName) =>
        InfoMemberName = (string.IsNullOrWhiteSpace(infoMemberName))
            ? null
            : infoMemberName.Trim();

    #region | ISimpleEnumInfoAttribute implementation |

    public Type InfoType => typeof(TInfo);
    public string InfoMemberName { get; }

    #endregion
}

public static class SimpleEnumHelper
{
    // ReSharper disable InconsistentNaming

    private static readonly object Locker = new();

    //Item1 = the Enum type
    private static readonly Dictionary<Type, Dictionary<string, object>> EnumDictionary = new();

    //Item1 = the SimpleEnumInfo type
    private static readonly Dictionary<Type, Dictionary<string, object>> InfoDictionary = new();

    // ReSharper restore InconsistentNaming

    private static bool CheckDictionaries(Type enumType = null, Type infoType = null)
    {
        var dictionariesExist = false;

        if (infoType != null)
        {
            if (InfoDictionary.ContainsKey(infoType))
            {
                dictionariesExist = true;
            }
        }
        else if (enumType != null)
        {
            if (EnumDictionary.ContainsKey(enumType))
            {
                dictionariesExist = true;
            }
        }

        if (((infoType != null) || (enumType != null)) && (!dictionariesExist))
        {
            lock (Locker)
            {
                do
                {
                    if (infoType != null && InfoDictionary.ContainsKey(infoType)) { break; }
                    if (enumType != null && EnumDictionary.ContainsKey(enumType)) { break; }

                    var dictionary = new Dictionary<string, object>();
                    PropertyInfo[] staticProps = null;

                    if (enumType == null)
                    {
                        //Need to get at least one instance of infoType
                        staticProps = infoType
                            .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                            .Where(w => w.PropertyType == infoType)
                            .ToArray();
                        if (staticProps.Length < 1) { break; }

                        foreach (var prop in staticProps)
                        {
                            if (prop.GetValue(infoType) is ISimpleEnumInfo info)
                            {
                                enumType = info.EnumType;
                                break;
                            }
                        }
                    }
                    if (enumType == null) { break; }

                    foreach (var member in Enum.GetValues(enumType))
                    {
                        var memberName = member.ToString();
                        if (memberName != null)
                        {
                            object memberInfo = null;
                            // ReSharper disable once ConstantConditionalAccessQualifier
                            var attribs = enumType
                                .GetMember(memberName)
                                .FirstOrDefault(f => f.DeclaringType == enumType)?
                                .GetCustomAttributes(true)?
                                .Where(w => w.GetType().IsAssignableTo(typeof(ISimpleEnumInfoAttribute)))
                                .Select(s => s as ISimpleEnumInfoAttribute)
                                .ToArray() ?? Array.Empty<ISimpleEnumInfoAttribute>();

                            if (attribs.Length > 1)
                            {
                                throw new TypeLoadException(
                                    $"The {enumType.Name}.{memberName} enum member cannot have more than one instance of SimpleEnumAttribute assigned to it.");
                            }
                            else if (attribs.Length == 1)
                            {
                                var attrib = attribs[0];
                                infoType ??= attrib.InfoType;

                                if (infoType != null && attrib.InfoType != null && infoType == attrib.InfoType)
                                {
                                    staticProps ??= infoType
                                        .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Where(w => w.PropertyType == infoType)
                                        .ToArray();

                                    if (!string.IsNullOrWhiteSpace(attrib.InfoMemberName))
                                    {
                                        var prop = staticProps.FirstOrDefault(f =>
                                            f.Name.Equals(attrib.InfoMemberName.Trim(),
                                                StringComparison.InvariantCultureIgnoreCase));
                                        if (prop != null)
                                        {
                                            if (prop.GetValue(infoType) is ISimpleEnumInfo info)
                                            {
                                                memberInfo = info;
                                            }
                                        }
                                    }
                                }
                            }
                            dictionary.Add(memberName, memberInfo);
                        }
                    }

                    EnumDictionary.Add(enumType, dictionary);
                    if (infoType != null)
                    {
                        InfoDictionary.Add(infoType, dictionary);
                    }

                    dictionariesExist = true;
                } while (false);
            }
        }

        return dictionariesExist;
    }

    public static TInfo FindMemberInfo<TInfo>(string memberName)
        where TInfo : class, ISimpleEnumInfo
    {
        TInfo result = null;

        if (!string.IsNullOrWhiteSpace(memberName))
        {
            var infoType = typeof(TInfo);

            if (CheckDictionaries(infoType: infoType) 
                && InfoDictionary.TryGetValue(infoType, out var dictionary))
            {
                if (dictionary.Any(a => a.Key.Equals(memberName.Trim(),
                        StringComparison.InvariantCultureIgnoreCase)
                    && a.Value != null))
                {
                    var kvp = dictionary.Single(s => s.Key.Equals(memberName.Trim(),
                                                                  StringComparison.InvariantCultureIgnoreCase)
                                                              && s.Value != null);
                    result = (TInfo)kvp.Value;
                }
            }
        }

        return result;
    }

    public static TInfo FindMemberInfo<TEnum, TInfo>(TEnum member)
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum
    {
        TInfo result = null;
        var enumType = typeof(TEnum);

        if (Enum.IsDefined(enumType, member))
        {
            var infoType = typeof(TInfo);

            if (CheckDictionaries(infoType: infoType) 
                && InfoDictionary.TryGetValue(infoType, out var dictionary))
            {
                if (dictionary.Any(a => a.Key.Equals(member.ToString(),
                                            StringComparison.InvariantCultureIgnoreCase)
                                        && a.Value != null))
                {
                    var kvp = dictionary.Single(s => s.Key.Equals(member.ToString(),
                                                         StringComparison.InvariantCultureIgnoreCase)
                                                     && s.Value != null);
                    var info = (TInfo)kvp.Value;
                    if (((ISimpleEnumInfo)info).EnumType == enumType)
                    {
                        result = info;
                    }
                }
            }
        }

        return result;
    }

    public static Dictionary<TEnum, TInfo> GetInfoDictionary<TEnum, TInfo>()
        where TInfo : class, ISimpleEnumInfo
        where TEnum : Enum
    {
        var result = new Dictionary<TEnum, TInfo>();

        var enumType = typeof(TEnum);
        var infoType = typeof(TInfo);

        if (CheckDictionaries(enumType: enumType, infoType: typeof(TInfo))
            && EnumDictionary.TryGetValue(enumType, out var dictionary))
        {
            foreach (var member in Enum.GetValues(enumType).Cast<TEnum>())
            {
                if (dictionary.Any(a => a.Key == member.ToString()
                                        && a.Value.GetType().IsAssignableTo(infoType)))
                {
                    var value = (TInfo)dictionary.Single(s => s.Key == member.ToString()
                                                    && s.Value.GetType().IsAssignableTo(infoType)).Value;
                    result.Add(member, value);
                }
                else
                {
                    result.Add(member, null);
                }
            }
        }

        return result;
    }
}

#endif

#if RESOLVE_SERVICES

public class SimpleServiceResolver
{
    private readonly IHost _host;

    // ReSharper disable once InconsistentNaming
    private static SimpleServiceResolver _instance;
    public static SimpleServiceResolver Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    $"The {nameof(SimpleServiceResolver)}.{nameof(CreateInstance)}() static method must be called at application start.");
            }

            return _instance;
        }
    }

    public static void CreateInstance(Action<IServiceCollection> configureServices, string[] args = null)
    {
        _instance = new SimpleServiceResolver(configureServices, args);
    }

    public static void CreateInstance(IHost host)
    {
        _instance = new SimpleServiceResolver(host);
    }

#if USING_WEB_HOST
    public static void CreateInstance(IWebHost webHost) => CreateInstance(new WebHostWrapper(webHost));
#endif

    private SimpleServiceResolver(Action<IServiceCollection> configureServices, string[] args = null)
    {
        var builder = (args == null)
            ? Host.CreateDefaultBuilder()
            : Host.CreateDefaultBuilder(args);

        _host = builder
            .ConfigureServices((context, services) =>
            {
                configureServices.Invoke(services);

#if SIMPLE_MESSAGING
                services.AddSimpleMessaging();
#endif

#if SIMPLE_HTTP_CLIENT
                services.AddSimpleHttpFactory();
#endif

            })
            .Build();
    }

    private SimpleServiceResolver(IHost host)
    {
        _host = host;
    }

    public async Task StartupHost() => await _host.StartAsync();

    public async Task ShutdownHost()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    public T GetService<T>() where T : class => _host.Services.GetRequiredService<T>();
    public IEnumerable<T> GetServices<T>() where T : class => _host.Services.GetServices<T>();
}

public static class SimpleServiceExtensions
{

#if SIMPLE_MESSAGING
    public static IServiceCollection AddSimpleMessaging(this IServiceCollection services)
    {
        SimpleMessaging.ConfigureServices(services);
        return services;
    }
#endif

#if SIMPLE_HTTP_CLIENT
    public static IServiceCollection AddSimpleHttpFactory(this IServiceCollection services)
    {
        SimpleHttpClientFactory.ConfigureServices(services);
        return services;
    }
#endif

}

#if USING_WEB_HOST

public class WebHostWrapper : IHost
{
    private readonly IWebHost _webHost;

    public IServiceProvider Services => _webHost.Services;

    public WebHostWrapper(IWebHost webHost)
    {
        _webHost = webHost;
    }

    #region | IHost implementation |

    public Task StartAsync(CancellationToken cancellationToken = new()) =>
        _webHost.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = new()) =>
        _webHost.StopAsync(cancellationToken);

    public void Dispose() => _webHost.Dispose();

    #endregion
}

#endif

#endif

#if SIMPLE_MESSAGING

public interface ISimpleMessaging
{
    void Send<TSender, TArgs>(TSender sender, string message, TArgs args)
        where TSender : class;

    void Send<TSender>(TSender sender, string message)
        where TSender : class;

    void Subscribe<TSender, TArgs>(object subscriber, string message, Action<TSender, TArgs> callback, TSender source)
        where TSender : class;

    void SubscribeFrom<TSender>(object subscriber, string message, Action<TSender> callback, TSender source)
        where TSender : class;

    void Subscribe<TArgs>(object subscriber, string message, Action<TArgs> callback);

    void Subscribe<TSender, TArgs>(object subscriber, string message, Func<TSender, TArgs, Task> callback, TSender source)
        where TSender : class;

    void SubscribeFrom<TSender>(object subscriber, string message, Func<TSender, Task> callback, TSender source)
        where TSender : class;

    void Subscribe<TArgs>(object subscriber, string message, Func<TArgs, Task> callback);

    void Unsubscribe<TSender, TArgs>(object subscriber, string message)
        where TSender : class;

    void UnsubscribeFrom<TSender>(object subscriber, string message)
        where TSender : class;

    void Unsubscribe<TArgs>(object subscriber, string message);
}

public class SimpleMessaging : ISimpleMessaging
{
    public static ISimpleMessaging Instance { get; } = new SimpleMessaging();

#if RESOLVE_SERVICES
    public static void ConfigureServices(IServiceCollection services)
    {
        if (services?.All(a => !typeof(ISimpleMessaging).IsAssignableFrom(a.ServiceType)) ?? false)
        {
            services.AddSingleton(Instance);
        }
    }
#endif

    private class Sender : Tuple<string, Type, Type>
    {
        public Sender(string message, Type senderType, Type argType)
            : base(message, senderType, argType) { }
    }

    private delegate bool Filter(object sender);

    private class MaybeWeakReference
    {
        WeakReference DelegateWeakReference { get; }
        object DelegateStrongReference { get; }

        readonly bool _isStrongReference;

        public MaybeWeakReference(object subscriber, object delegateSource)
        {
            if (subscriber.Equals(delegateSource))
            {
                // The target is the subscriber; we can use a weak reference
                DelegateWeakReference = new WeakReference(delegateSource);
                _isStrongReference = false;
            }
            else
            {
                DelegateStrongReference = delegateSource;
                _isStrongReference = true;
            }
        }

        public object Target => _isStrongReference ? DelegateStrongReference : DelegateWeakReference?.Target;
        public bool IsAlive => _isStrongReference
                               || (DelegateWeakReference?.IsAlive ?? false);
    }

    private class Subscription : IDisposable
    {
        public Subscription(
            object subscriber,
            object delegateSource,
            MethodInfo syncMethod,
            Func<object, object, Task> asyncMethod,
            Filter filter)
        {
            Subscriber = new WeakReference(subscriber);
            DelegateSource = new MaybeWeakReference(subscriber, delegateSource);
            SyncMethod = syncMethod;
            AsyncMethod = asyncMethod;
            Filter = filter;
        }

        public WeakReference Subscriber { get; }
        private MaybeWeakReference DelegateSource { get; }
        private MethodInfo SyncMethod { get; set; }
        private Func<object, object, Task> AsyncMethod { get; set; }
        private Filter Filter { get; }
        private SemaphoreSlim _asyncLocker = new(1, 1);
        private bool _isDisposed;

        public void InvokeCallback(object sender, object args)
        {
            if (_isDisposed) { return; }

            if (sender != null && (!Filter(sender))) { return; }

            if (AsyncMethod != null)
            {
                //Because of the nature of Subscription Callbacks, we must always invoke async subscription callback functions
                //  as fire-and-forget - there is no way to actually await them - and any UI thread-affecting code in the
                //  callback will always need to be run as "InvokeOnMainThread".
                //  They will NOT be invoked in a thread-safe way - thread-safety must be ensured by the 

                // ReSharper disable once AsyncVoidLambda
                new Task(async () =>
                {
                    try
                    {
                        if (!_isDisposed)
                        {
                            await _asyncLocker.WaitAsync();
                        }

                        if (!_isDisposed)
                        {
                            await AsyncMethod.Invoke(sender, args);
                        }
                    }
                    finally
                    {
                        if (!_isDisposed)
                        {
                            _asyncLocker.Release();
                        }
                    }

                }).Start();
            }
            else if (SyncMethod != null)
            {
                if (SyncMethod.IsStatic)
                {
                    SyncMethod.Invoke(null, 
                        (SyncMethod.GetParameters().Length == 1)
                            ? [sender ?? args]
                            : [sender, args]);
                    return;
                }

                var target = DelegateSource.Target;

                if (target == null) { return; }

                SyncMethod.Invoke(target, 
                    (SyncMethod.GetParameters().Length == 1)
                        ? [sender ?? args]
                        : [sender, args]);
            }
        }

        public bool CanBeRemoved()
        {
            return (!Subscriber.IsAlive) || (!DelegateSource.IsAlive);
        }

        #region | IDisposable implementation |

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                SyncMethod = null;
                AsyncMethod = null;
                _asyncLocker?.Dispose();
                _asyncLocker = null;
            }
        }

        #endregion
    }

    private readonly Dictionary<Sender, List<Subscription>> _subscriptions = new();
    private readonly object _subscriptionLocker = new();

    private void InnerSend(
        string message,
        Type senderType,
        Type argType,
        object sender,
        object args)
    {
        if (message == null) { throw new ArgumentNullException(nameof(message)); }

        //Item1 = the subscription
        //Item2 = is it explicit?
        var matchingSubscriptions = new List<Tuple<Subscription, bool>>();
        var typedSubscriptions = new List<Subscription>();
        var genericSubscriptions = new List<Subscription>();

        lock (_subscriptionLocker)
        {
            // Step 1 - look for subscriptions that explicitly reference this senderType
            var key = new Sender(message, senderType, argType);
            if (_subscriptions.TryGetValue(key, out var typedSubs))
            {
                typedSubscriptions.AddRange(typedSubs);
                matchingSubscriptions.AddRange(typedSubscriptions.Select(s => Tuple.Create(s, true)));
            }

            //Step 2 - look for subscriptions that reference the generic 'object' senderType
            key = new Sender(message, typeof(object), argType);
            if (_subscriptions.TryGetValue(key, out var genericSubs))
            {
                genericSubscriptions.AddRange(genericSubs);
                matchingSubscriptions.AddRange(genericSubscriptions.Select(s => Tuple.Create(s, false)));
            }
        }

        foreach (var subscription in matchingSubscriptions)
        {
            if (subscription.Item1.Subscriber.Target != null
                && (typedSubscriptions.Contains(subscription.Item1) || genericSubscriptions.Contains(subscription.Item1)))
            {
                //If the senderType was explicitly referenced, send the 'sender' - otherwise send null
                subscription.Item1.InvokeCallback((subscription.Item2 ? sender : null), args);
            }
        }
    }

    private void InnerSubscribe(
        object subscriber,
        string message,
        Type senderType,
        Type argType,
        object target,
        MethodInfo syncMethod,
        Func<object, object, Task> asyncMethod,
        Filter filter)
    {
        if (message == null) { throw new ArgumentNullException(nameof(message)); }

        var key = new Sender(message, senderType, argType);
        var value = new Subscription(subscriber, target, syncMethod, asyncMethod, filter);

        lock (_subscriptionLocker)
        {
            if (_subscriptions.TryGetValue(key, out var subs))
            {
                subs.Add(value);
            }
            else
            {
                _subscriptions.Add(key, [value]);
            }
        }
    }

    private void InnerUnsubscribe(
        string message,
        Type senderType,
        Type argType,
        object subscriber)
    {
        if (subscriber == null) { throw new ArgumentNullException(nameof(subscriber)); }
        if (message == null) { throw new ArgumentNullException(nameof(message)); }

        var key = new Sender(message, senderType, argType);
        lock (_subscriptionLocker)
        {
            if (_subscriptions.TryGetValue(key, out var subs))
            {
                var toRemove = subs.Where(w => w.CanBeRemoved()
                                               || w.Subscriber.Target == subscriber).ToArray();
                Array.ForEach(toRemove, f =>
                {
                    f?.Dispose();
                    subs.Remove(f);
                });
                if (!subs.Any()) { _subscriptions.Remove(key); }
            }
        }
    }

    #region Static methods - only want these available in non-ServiceResolver scenarios

#if !RESOLVE_SERVICES
    public static void Send<TSender, TArgs>(TSender sender, string message, TArgs args) 
        where TSender : class =>
        Instance.Send(sender, message, args);
    
    public static void Send<TSender>(TSender sender, string message) 
        where TSender : class =>
        Instance.Send(sender, message);
    
    public static void Subscribe<TSender, TArgs>(
        object subscriber, 
        string message, 
        Action<TSender, TArgs> callback, 
        TSender source) 
        where TSender : class =>
        Instance.Subscribe(subscriber, message, callback, source);
    
    public static void SubscribeFrom<TSender>(
        object subscriber, 
        string message, 
        Action<TSender> callback, 
        TSender source) where TSender : class =>
        Instance.SubscribeFrom(subscriber, message, callback, source);

    public static void Subscribe<TArgs>(
        object subscriber,
        string message,
        Action<TArgs> callback) =>
        Instance.Subscribe(subscriber, message, callback);

    public static void Subscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Func<TSender, TArgs, Task> callback,
        TSender source)
        where TSender : class =>
        Instance.Subscribe(subscriber, message, callback, source);

    public static void SubscribeFrom<TSender>(
        object subscriber,
        string message,
        Func<TSender, Task> callback,
        TSender source) where TSender : class =>
        Instance.SubscribeFrom(subscriber, message, callback, source);

    public static void Subscribe<TArgs>(
        object subscriber,
        string message,
        Func<TArgs, Task> callback) =>
        Instance.Subscribe(subscriber, message, callback);

    public static void Unsubscribe<TSender, TArgs>(object subscriber, string message) 
        where TSender : class =>
        Instance.Unsubscribe<TSender, TArgs>(subscriber, message);
    
    public static void UnsubscribeFrom<TSender>(object subscriber, string message) 
        where TSender : class =>
        Instance.UnsubscribeFrom<TSender>(subscriber, message);
    
    public static void Unsubscribe<TArgs>(object subscriber, string message) =>
        Instance.Unsubscribe<TArgs>(subscriber, message);
#endif

    #endregion

    #region | ISimpleMessaging implementation |

    void ISimpleMessaging.Send<TSender, TArgs>(TSender sender, string message, TArgs args)
    {
        if (sender == null) { throw new ArgumentNullException(nameof(sender)); }
        InnerSend(message, typeof(TSender), typeof(TArgs), sender, args);
    }

    void ISimpleMessaging.Send<TSender>(TSender sender, string message)
    {
        if (sender == null) { throw new ArgumentNullException(nameof(sender)); }
        InnerSend(message, typeof(TSender), null, sender, null);
    }

    void ISimpleMessaging.Subscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Action<TSender, TArgs> callback,
        TSender source)
        where TSender : class
    {
        if (subscriber == null) { throw new ArgumentNullException(nameof(subscriber)); }
        if (callback == null) { throw new ArgumentNullException(nameof(callback)); }

        InnerSubscribe(subscriber, message, typeof(TSender), typeof(TArgs), callback.Target, callback.GetMethodInfo(), null,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.SubscribeFrom<TSender>(
        object subscriber,
        string message,
        Action<TSender> callback,
        TSender source)
        where TSender : class
    {
        if (subscriber == null) { throw new ArgumentNullException(nameof(subscriber)); }
        if (callback == null) { throw new ArgumentNullException(nameof(callback)); }

        InnerSubscribe(subscriber, message, typeof(TSender), null, callback.Target, callback.GetMethodInfo(), null,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.Subscribe<TArgs>(object subscriber, string message, Action<TArgs> callback)
    {
        if (subscriber == null) { throw new ArgumentNullException(nameof(subscriber)); }
        if (callback == null) { throw new ArgumentNullException(nameof(callback)); }

        InnerSubscribe(subscriber, message, typeof(object), typeof(TArgs), callback.Target, callback.GetMethodInfo(), null,
            filter: _ => true); //filter won't be used, for 'generic' object subscriptions
    }

    void ISimpleMessaging.Subscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Func<TSender, TArgs, Task> callback,
        TSender source)
        where TSender : class
    {
        if (subscriber == null) { throw new ArgumentNullException(nameof(subscriber)); }
        if (callback == null) { throw new ArgumentNullException(nameof(callback)); }

        Task AsyncMethod(object sender, object args)
        {
            if (sender != null && sender.GetType().IsAssignableTo(typeof(TSender)))
            {
                var typedArgs = (args != null && args.GetType().IsAssignableTo(typeof(TArgs)))
                    ? (TArgs)args
                    : default;
                return callback.Invoke((TSender)sender, typedArgs);
            }

            return Task.Run(() => { });
        }

        InnerSubscribe(subscriber, message, typeof(TSender), typeof(TArgs), callback.Target, null, AsyncMethod,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.SubscribeFrom<TSender>(
        object subscriber,
        string message,
        Func<TSender, Task> callback,
        TSender source)
        where TSender : class
    {
        if (subscriber == null) { throw new ArgumentNullException(nameof(subscriber)); }
        if (callback == null) { throw new ArgumentNullException(nameof(callback)); }

        Task AsyncMethod(object sender, object args)
        {
            var typedSender = (sender != null && sender.GetType().IsAssignableTo(typeof(TSender)))
                ? (TSender)sender
                : (args != null && args.GetType().IsAssignableTo(typeof(TSender)))
                    ? (TSender)args
                    : null;
            return (typedSender != null)
                ? callback.Invoke(typedSender)
                : Task.Run(() => { });
        }

        InnerSubscribe(subscriber, message, typeof(TSender), null, callback.Target, null, AsyncMethod,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.Subscribe<TArgs>(object subscriber, string message, Func<TArgs, Task> callback)
    {
        if (subscriber == null) { throw new ArgumentNullException(nameof(subscriber)); }
        if (callback == null) { throw new ArgumentNullException(nameof(callback)); }

        Task AsyncMethod(object sender, object args)
        {
            if (args != null && args.GetType().IsAssignableTo(typeof(TArgs)))
            {
                return callback.Invoke((TArgs)args);
            }

            if (sender != null && sender.GetType().IsAssignableTo(typeof(TArgs)))
            {
                return callback.Invoke((TArgs)sender);
            }

            return Task.Run(() => { });
        }

        InnerSubscribe(subscriber, message, typeof(object), typeof(TArgs), callback.Target, null, AsyncMethod,
            filter: _ => true); //filter won't be used, for 'generic' object subscriptions
    }

    void ISimpleMessaging.Unsubscribe<TSender, TArgs>(object subscriber, string message) =>
        InnerUnsubscribe(message, typeof(TSender), typeof(TArgs), subscriber);

    void ISimpleMessaging.UnsubscribeFrom<TSender>(object subscriber, string message) =>
        InnerUnsubscribe(message, typeof(TSender), null, subscriber);

    void ISimpleMessaging.Unsubscribe<TArgs>(object subscriber, string message) =>
        InnerUnsubscribe(message, typeof(object), typeof(TArgs), subscriber);

    #endregion
}

#endif

#if SIMPLE_HTTP_CLIENT

public interface ISimpleHttpClientFactory : IHttpClientFactory
{
    SimpleHttpClient CreateSimpleClient(string name,
        string baseUrl = null,
        string bearerToken = null);
}

public class SimpleHttpClientFactory : ISimpleHttpClientFactory
{

#if RESOLVE_SERVICES
    public static void ConfigureServices(IServiceCollection services)
    {
        if (services != null)
        {
            if (services.All(a => !typeof(IHttpClientFactory).IsAssignableFrom(a.ServiceType)))
            {
                var factory = new SimpleHttpClientFactory();
                services.AddSingleton<IHttpClientFactory>(factory);
                services.AddSingleton<ISimpleHttpClientFactory>(factory);
            }
            else if (services.All(a => !typeof(ISimpleHttpClientFactory).IsAssignableFrom(a.ServiceType)))
            {
                var factory = new SimpleHttpClientFactory();
                services.AddSingleton<ISimpleHttpClientFactory>(factory);
            }
        }
    }
#endif

    private class HttpClientReference
    {
        public int ReferenceCount { get; private set; }
        public HttpClientHandler Handler { get; private set; }

        public HttpClientReference(HttpClientHandler handler)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void IncrementReferenceCount() => ReferenceCount++;

        public void DecrementReferenceCount()
        {
            ReferenceCount--;
            if (ReferenceCount < 1)
            {
                Handler?.Dispose();
                Handler = null;
            }
        }
    }

    private readonly Dictionary<string, HttpClientReference> _handlers = new();
    private readonly object _handlerLocker = new();

    private SimpleHttpClient GetClient(string name)
    {
        SimpleHttpClient result;

        var key = (string.IsNullOrWhiteSpace(name)) ? string.Empty : name.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(key))
        {
            //No name - using disposable client handler
            result = new SimpleHttpClient(name, this, new HttpClientHandler());
        }
        else
        {
            lock (_handlerLocker)
            {
                HttpClientHandler handler = null;

                if (_handlers.ContainsKey(key))
                {
                    var reference = _handlers[key];
                    if (reference.ReferenceCount > 0)
                    {
                        reference.IncrementReferenceCount();
                        handler = reference.Handler;
                    }
                    else
                    {
                        reference.DecrementReferenceCount();
                        _handlers.Remove(key);
                    }
                }

                if (handler == null)
                {
                    var reference = new HttpClientReference(new HttpClientHandler());
                    reference.IncrementReferenceCount();
                    _handlers.Add(key, reference);
                    handler = reference.Handler;
                }

                result = new SimpleHttpClient(name, this, handler);
            }
        }

        return result;
    }

    public void DeregisterClient(string name)
    {
        var key = (string.IsNullOrWhiteSpace(name)) ? string.Empty : name.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(key))
        {
            //Nothing to do, client not registered
        }
        else
        {
            lock (_handlerLocker)
            {
                if (_handlers.ContainsKey(key))
                {
                    var reference = _handlers[key];
                    reference.DecrementReferenceCount();
                    if (reference.ReferenceCount < 1)
                    {
                        _handlers.Remove(key);
                    }
                }
            }
        }
    }

    #region | ISimpleHttpClientFactory implementation |

    public SimpleHttpClient CreateSimpleClient(string name,
        string baseUrl = null,
        string bearerToken = null)
    {
        var result = GetClient(name);

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            result.BaseAddress = new Uri(baseUrl.Trim());
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            result.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", 
                bearerToken.Trim());
        }

        return result;
    }

    #endregion

    #region | IHttpClientFactory implementation |

    public HttpClient CreateClient(string name) => GetClient(name);

    #endregion
}

public class SimpleHttpClient : HttpClient, IDisposable
{
    // ReSharper disable once InconsistentNaming
    private static readonly string JsonMediaType = "application/json";

    private bool _isDisposed;
    private readonly string _clientName;
    private SimpleHttpClientFactory _factory;

    private static JsonSerializerOptions SaveJsonOptions { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string _jsonSaveFolder;
    /// <summary>
    /// If this property is set to a valid directory path, http request operations
    /// will try to save JSON request and response data to .json files in the
    /// specified folder.
    /// </summary>
    public string JsonSaveFolder
    {
        get => _jsonSaveFolder;
        set => _jsonSaveFolder = (string.IsNullOrWhiteSpace(value)) ? null : value.Trim();
    }

    private string GetQueryPropertyValue(object queryParams, PropertyInfo property)
    {
        var result = string.Empty;

        if (queryParams != null && property != null)
        {
            var value = property.GetValue(queryParams)?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                result = value.Trim();
            }
        }

        return result;
    }

    private string GetApiPath(string api, object queryParams)
    {
        var result = string.Empty;

        if (!string.IsNullOrWhiteSpace(api))
        {
            result += api.Trim();

            if (queryParams != null)
            {
                var properties = queryParams.GetType().GetProperties();
                var paramValues = properties.Select(s =>
                    $"{s.Name.ToLowerInvariant()}={GetQueryPropertyValue(queryParams, s)}");
                var query = $"{string.Join('&', paramValues)}";

                if (!string.IsNullOrWhiteSpace(query))
                {
                    result += $"?{query.Trim()}";
                }
            }
        }

        return result;
    }

    public SimpleHttpClient(string name, SimpleHttpClientFactory factory, HttpClientHandler handler)
        : base(handler, disposeHandler: string.IsNullOrWhiteSpace(name))
    {
        _clientName = (string.IsNullOrWhiteSpace(name)) ? string.Empty : name.Trim().ToLowerInvariant();
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        if (handler == null) { throw new ArgumentNullException(nameof(handler));}
    }

    private byte[] GetFormattedJsonBytes(string json)
    {
        byte[] result = null;

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var parsed = JsonDocument.Parse(json.Trim());
                var formatted = JsonSerializer.Serialize(parsed, SaveJsonOptions);
                if (!string.IsNullOrWhiteSpace(formatted))
                {
                    result = Encoding.UTF8.GetBytes(formatted.Trim());
                }
            }
            catch (Exception)
            {
                //Nothing to do here, can't parse the json
            }
        }

        return (result is { Length: > 0 }) ? result : null;
    }

    private async Task TrySaveRequest(string requestJson, DateTime requestTimestamp, [CallerMemberName] string caller = null)
    {
        if ((!string.IsNullOrWhiteSpace(requestJson))
            && JsonSaveFolder != null
            && IO.Directory.Exists(JsonSaveFolder))
        {
            var jsonBytes = GetFormattedJsonBytes(requestJson);
            if (jsonBytes != null)
            {
                try
                {
                    var filePath = IO.Path.Combine(JsonSaveFolder, $"{requestTimestamp.Ticks}"
                                   + $"_{((string.IsNullOrWhiteSpace(caller)) ? "HttpOperation" : caller.Trim())}"
                                   + "_Request.json");
                    await using var fs = new IO.FileStream(filePath, IO.FileMode.CreateNew, IO.FileAccess.ReadWrite);
                    await fs.WriteAsync(jsonBytes, 0, jsonBytes.Length, default);
                }
                catch (Exception)
                {
                    //Nothing to do here, can't write the file
                }
            }
        }
    }

    private async Task TrySaveResponse(string responseJson, DateTime requestTimestamp, [CallerMemberName] string caller = null)
    {
        if ((!string.IsNullOrWhiteSpace(responseJson))
            && JsonSaveFolder != null
            && IO.Directory.Exists(JsonSaveFolder))
        {
            var jsonBytes = GetFormattedJsonBytes(responseJson);
            if (jsonBytes != null)
            {
                try
                {
                    var filePath = IO.Path.Combine(JsonSaveFolder, $"{requestTimestamp.Ticks}"
                                                                   + $"_{((string.IsNullOrWhiteSpace(caller)) ? "HttpOperation" : caller.Trim())}"
                                                                   + "_Response.json");
                    await using var fs = new IO.FileStream(filePath, IO.FileMode.CreateNew, IO.FileAccess.ReadWrite);
                    await fs.WriteAsync(jsonBytes, 0, jsonBytes.Length, default);
                }
                catch (Exception)
                {
                    //Nothing to do here, can't write the file
                }
            }
        }
    }

    private void CheckDisposed([CallerMemberName] string caller = null)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException((string.IsNullOrWhiteSpace(caller))
                ? $"{nameof(SimpleHttpClient)} instance has been disposed."
                : $"Cannot call {caller.Trim()} on a {nameof(SimpleHttpClient)} instance that has been disposed.");
        }
    }

    #region | GET and POST operations |

    public async Task<TResponse> GetResponseAsync<TResponse>(string api, object queryParams = null)
    where TResponse : class
    {
        CheckDisposed();

        TResponse result = null;

        if (!string.IsNullOrWhiteSpace(api))
        {
            var response = await GetAsync(GetApiPath(api, queryParams));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            await TrySaveResponse(json, DateTime.Now);
            if (!string.IsNullOrWhiteSpace(json))
            {
                result = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
            }
        }

        return result;
    }

    public async Task GetNoResponseAsync(string api, object queryParams = null)
    {
        CheckDisposed();

        if (!string.IsNullOrWhiteSpace(api))
        {
            var response = await GetAsync(GetApiPath(api, queryParams));
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<TResponse> PostWithResponseAsync<TRequest, TResponse>(string api, TRequest data, object queryParams = null)
        where TRequest : class
        where TResponse : class
    {
        CheckDisposed();

        TResponse result = null;

        if ((!string.IsNullOrWhiteSpace(api)) && data != null)
        {
            var timestamp = DateTime.Now;
            var request = JsonContent.Create(data);
            if (JsonSaveFolder != null)
            {
                await TrySaveRequest((await request.ReadAsStringAsync()), timestamp);
            }
            var response = await PostAsync(GetApiPath(api, queryParams), request);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            await TrySaveResponse(responseJson, timestamp);
            if (!string.IsNullOrWhiteSpace(responseJson))
            {
                result = JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
            }
        }

        return result;
    }

    public async Task PostWithNoResponseAsync<TRequest>(string api, TRequest data, object queryParams = null)
        where TRequest : class
    {
        CheckDisposed();

        if ((!string.IsNullOrWhiteSpace(api)) && data != null)
        {
            var request = JsonContent.Create(data);
            if (JsonSaveFolder != null)
            {
                await TrySaveRequest((await request.ReadAsStringAsync()), DateTime.Now);
            }
            var response = await PostAsync(GetApiPath(api, queryParams), request);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<TResponse> PostJsonWithResponseAsync<TResponse>(string api, string data, object queryParams = null)
        where TResponse : class
    {
        CheckDisposed();

        TResponse result = null;

        if ((!string.IsNullOrWhiteSpace(api)) && data != null)
        {
            var timestamp = DateTime.Now;
            var request = new StringContent(data, Encoding.UTF8, JsonMediaType);
            if (JsonSaveFolder != null)
            {
                await TrySaveRequest((await request.ReadAsStringAsync()), timestamp);
            }
            var response = await PostAsync(GetApiPath(api, queryParams), request);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            await TrySaveResponse(responseJson, timestamp);
            if (!string.IsNullOrWhiteSpace(responseJson))
            {
                result = JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
            }
        }

        return result;
    }

    #endregion

    #region | IDisposable implementation |

    public new void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            base.Dispose();
            _factory?.DeregisterClient(_clientName);
            _factory = null;
        }
    }

    #endregion
}

#endif

#region SimpleOsInfo

public enum IdentifiedLinuxDistro
{
    Unknown = 0,
    Alpine,
    Debian,
    Ubuntu,
    Mint,
    // ReSharper disable once InconsistentNaming
    LMDE,
    Android,
    NotLinux = 999,
}

public class RunUnixShellCommandResult
{
    public RunUnixShellCommandResult(bool isComplete = false)
    {
        IsComplete = isComplete;
    }
    
    public string Output { get; set; }
    public string Error { get; set; }
    public Exception Exception { get; set; }
    public bool IsComplete { get; private set; }

    public bool IsError => (!string.IsNullOrWhiteSpace(Error))
                           || Exception != null;

    public bool IsEmptyOutput => (string.IsNullOrWhiteSpace(Output));

    public string[] OutputLines =>
        (string.IsNullOrWhiteSpace(Output))
            ? [string.Empty]
            : Output.Split(SimpleOsInfo.UnixLineSplitters, StringSplitOptions.RemoveEmptyEntries);

    public void SetComplete() => IsComplete = true;
}

public class OsVersionInfo
{
    public string VersionNumber { get; set; }
    public int MajorVersion { get; set; } //This is the first version value X.y.y.y
    public int? MinorVersion { get; set; } //This is the second version value y.X.y.y
    public int? BuildVersion { get; set; } //This is the third version value y.y.X.y
    public int? RevisionVersion { get; set; } //This is the fourth version value y.y.y.X
    public bool? IsLongTermSupported { get; set; }
    public string VersionCodename { get; set; }
    public string BasedOnVersion { get; set; }
    public string ProductName { get; set; }
    public string ProductNameDisplay { get; set; }

    public string FullVersion
    {
        get
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(VersionNumber))
            {
                sb.Append(VersionNumber + " ");
            }
            else if (MajorVersion > 0 || MinorVersion.HasValue)
            {
                sb.Append(MajorVersion.ToString());
                if (MinorVersion.HasValue)
                {
                    sb.Append($".{MinorVersion.Value}");
                    if (BuildVersion.HasValue)
                    {
                        sb.Append($".{BuildVersion.Value}");
                        if (RevisionVersion.HasValue)
                        {
                            sb.Append($".{RevisionVersion.Value}");
                        }
                    }
                }

                sb.Append(' ');
            }

            if ((IsLongTermSupported is true)
                && (string.IsNullOrWhiteSpace(VersionNumber)
                    || (!VersionNumber.Contains("LTS", StringComparison.InvariantCultureIgnoreCase))))
            {
                sb.Append("LTS ");
            }

            var details = string.Empty;

            if ((!string.IsNullOrWhiteSpace(VersionCodename))
                && (string.IsNullOrWhiteSpace(VersionNumber)
                    || (!VersionNumber.Contains(VersionCodename.Trim(), StringComparison.InvariantCultureIgnoreCase))))
            {
                details += VersionCodename.Trim();
            }

            if (!string.IsNullOrWhiteSpace(BasedOnVersion))
            {
                details += (string.IsNullOrWhiteSpace(details))
                    ? string.Empty
                    : " - ";
                details += $"based on: {BasedOnVersion.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(details))
            {
                sb.Append($"({details.Trim()}) ");
            }

            return sb.ToString().Trim();
        }
    }
}

public class SimpleOsInfo
{
    public static char[] UnixLineSplitters { get; } = ['\r', '\n'];

    private static IdentifiedLinuxDistro[] DebianStyleDistros { get; } =
    [
        IdentifiedLinuxDistro.Debian, 
        IdentifiedLinuxDistro.LMDE,
        IdentifiedLinuxDistro.Mint, 
        IdentifiedLinuxDistro.Ubuntu
    ];

    //The following list is from here:
    //  https://en.wikipedia.org/wiki/MacOS_version_history
    //  This list was last retrieved and updated: 8/10/2024
    private static (int DarwinVersion, string Codename)[] MacOsCodenames { get; } =
        [
            (6, "Jaguar"),
            (7, "Panther"),
            (8, "Tiger"),
            (9, "Leopard"),
            (10, "Snow Leopard"),
            (11, "Lion"),
            (12, "Mountain Lion"),
            (13, "Mavericks"),
            (14, "Yosemite"),
            (15, "El Capitan"),
            (16, "Sierra"),
            (17, "High Sierra"),
            (18, "Mojave"),
            (19, "Catalina"),
            (20, "Big Sur"),
            (21, "Monterey"),
            (22, "Ventura"),
            (23, "Sonoma"),
            (24, "Sequoia"),
        ];

    private static Dictionary<int, (Version Version, string Codename)> AndroidCodenames { get; } = new()
    {
        { 1, new ValueTuple<Version, string>(new Version(1, 0), "Initial") },
        { 2, new ValueTuple<Version, string>(new Version(1, 1), "Petit Four") },
        { 3, new ValueTuple<Version, string>(new Version(1, 5), "Cupcake") },
        { 4, new ValueTuple<Version, string>(new Version(1, 6), "Donut") },
        { 5, new ValueTuple<Version, string>(new Version(2, 0), "Eclair") },
        { 6, new ValueTuple<Version, string>(new Version(2,0,1), "Eclair") },
        { 7, new ValueTuple<Version, string>(new Version(2, 1), "Eclair") },
        { 8, new ValueTuple<Version, string>(new Version(2, 2), "Froyo") },
        { 9, new ValueTuple<Version, string>(new Version(2, 3), "Gingerbread") },
        { 10, new ValueTuple<Version, string>(new Version(2, 3, 3), "Gingerbread") },
        { 11, new ValueTuple<Version, string>(new Version(3, 0), "Honeycomb") },
        { 12, new ValueTuple<Version, string>(new Version(3, 1), "Honeycomb") },
        { 13, new ValueTuple<Version, string>(new Version(3, 2), "Honeycomb") },
        { 14, new ValueTuple<Version, string>(new Version(4, 0), "Ice Cream Sandwich") },
        { 15, new ValueTuple<Version, string>(new Version(4, 0, 3), "Ice Cream Sandwich") },
        { 16, new ValueTuple<Version, string>(new Version(4, 1), "Jelly Bean") },
        { 17, new ValueTuple<Version, string>(new Version(4, 2), "Jelly Bean") },
        { 18, new ValueTuple<Version, string>(new Version(4, 3), "Jelly Bean") },
        { 19, new ValueTuple<Version, string>(new Version(4, 4), "KitKat") },
        { 20, new ValueTuple<Version, string>(new Version(4, 5), "KitKat") },
        { 21, new ValueTuple<Version, string>(new Version(5, 0), "Lollipop") },
        { 22, new ValueTuple<Version, string>(new Version(5, 1), "Lollipop") },
        { 23, new ValueTuple<Version, string>(new Version(6, 0), "Marshmallow") },
        { 24, new ValueTuple<Version, string>(new Version(7, 0), "Nougat") },
        { 25, new ValueTuple<Version, string>(new Version(7, 1), "Nougat") },
        { 26, new ValueTuple<Version, string>(new Version(8, 0), "Oreo") },
        { 27, new ValueTuple<Version, string>(new Version(8, 1), "Oreo") },
        { 28, new ValueTuple<Version, string>(new Version(9, 0), "Pie") },
        { 29, new ValueTuple<Version, string>(new Version(10, 0), "Quince Tart") },
        { 30, new ValueTuple<Version, string>(new Version(11, 0), "Red Velvet Cake") },
        { 31, new ValueTuple<Version, string>(new Version(12, 0), "Snow Cone") },
        { 32, new ValueTuple<Version, string>(new Version(12, 1), "Snow Cone V2") },
        { 33, new ValueTuple<Version, string>(new Version(13, 0), "Tiramisu") },
        { 34, new ValueTuple<Version, string>(new Version(14, 0), "Upside Down Cake") },
        { 35, new ValueTuple<Version, string>(new Version(15, 0), "Vanilla Ice Cream") },
    };

    private const string UnixRootUsername = "root";
    private const string DebianDistroIdentifier = "debian";
    private const string LmdeDistroIdentifier = "lmde";
    private const string MintDistroIdentifier = "mint"; //TODO: Not sure this is correct
    private const string UbuntuDistroIdentifier = "ubuntu";
    private const string AlpineDistroIdentifier = "alpine linux";

    public const string LinuxListTextFileCommand = "cat";
    public const string LinuxIdentifyDistroArgs = "/etc/issue";
    public const string DebianVersionArgs = "/etc/debian_version";
    public const string LinuxOsReleaseDetailsArgs = "/etc/os-release";

    public const string MacOsInfoCommand = "system_profiler";
    public const string MacOsInfoCommandArgs = "SPSoftwareDataType";

    public const string UnixUsernameCommand = "whoami";

    public static Task<SimpleOsInfo> GatherInfo(bool withConsoleOutput = false) =>
        (new SimpleOsInfo()).Gather(withConsoleOutput);

    private (int MajorVersion, int? MinorVersion, int? BuildVersion, int? RevisionVersion) GetVersionParts(string versionText)
    {
        var majorVersion = 0;
        int? minorVersion, buildVersion;
        var revisionVersion = buildVersion = minorVersion = null;

        if (!string.IsNullOrWhiteSpace(versionText))
        {
            var versionParts = versionText.Trim().Split('.');
            
            if (versionParts.Length > 0 && int.TryParse(versionParts[0], out var major))
            {
                majorVersion = major;

                if (versionParts.Length > 1 && int.TryParse(versionParts[1], out var minor))
                {
                    minorVersion = minor;

                    if (versionParts.Length > 2 && int.TryParse(versionParts[2], out var build))
                    {
                        buildVersion = build;
                            
                        if (versionParts.Length > 3 && int.TryParse(versionParts[3], out var revision))
                        {
                            revisionVersion = revision;
                        }
                    }
                }
            }
        }

        return (majorVersion, minorVersion, buildVersion, revisionVersion);
    }
    
    private Dictionary<string, string> GetInfoDictionary(IList<string> infoLines, char assignmentChar)
    {
        var result = new Dictionary<string, string>();

        if (infoLines != null)
        {
            foreach (var line in infoLines
                         .Where(w => (!string.IsNullOrWhiteSpace(w)) && w.Contains(assignmentChar))
                         .Select(s => s.Trim()))
            {
                var lineParts = line.Split(assignmentChar);
                if (lineParts.Length > 1)
                {
                    var key = lineParts[0];
                    var value = string.Join(assignmentChar, lineParts[1..]);

                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.TrimStart('\"').TrimEnd('\"').Trim();
                    }

                    _ = result.TryAdd(key, value);
                }
            }
        }

        return result;
    }
    
    private async Task<OsVersionInfo> GetDebianVersionInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;
        
        if (IsLinux && distro == IdentifiedLinuxDistro.Debian)
        {
            result = new OsVersionInfo();

            do
            {
                #region | Get the best possible version info |
                
                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, DebianVersionArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var version = shellResult.OutputLines[0].Trim().Split(' ').FirstOrDefault();
                if (string.IsNullOrWhiteSpace(version)) {break;}

                result.VersionNumber = version;

                var versionParts = GetVersionParts(version);
                result.MajorVersion = versionParts.MajorVersion;
                result.MinorVersion = versionParts.MinorVersion;
                result.BuildVersion = versionParts.BuildVersion;
                result.RevisionVersion = versionParts.RevisionVersion;
                
                #endregion

                #region | Get the os version codename and product name |
                
                shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxOsReleaseDetailsArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, '=');
                
                if (!(infoDictionary?.Any() ?? false)) { break; }

                if (infoDictionary.TryGetValue("VERSION_CODENAME", out var codename)
                    && (!string.IsNullOrWhiteSpace(codename)))
                {
                    result.VersionCodename = codename.Trim();
                }

                if (infoDictionary.TryGetValue("NAME", out var name)
                    && (!string.IsNullOrWhiteSpace(name)))
                {
                    result.ProductName = name.Trim();
                }

                if (infoDictionary.TryGetValue("PRETTY_NAME", out var displayName)
                    && (!string.IsNullOrWhiteSpace(displayName)))
                {
                    result.ProductNameDisplay = displayName.Trim();
                }

                #endregion
                
            } while (false);
        }

        return result;
    }

    private async Task GetDebianUserInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        if (IsLinux && DebianStyleDistros.Contains(distro))
        {
            var shellResult = await RunUnixShellCommand(UnixUsernameCommand, 
                showOutput: withConsoleOutput);
            if (shellResult.IsComplete && (!shellResult.IsError) && (!shellResult.IsEmptyOutput))
            {
                RunningAsUser = shellResult.OutputLines[0].Trim();
                IsAdminUser = IsUnixRootUser(RunningAsUser);
            }
        }
    }

    private async Task<OsVersionInfo> GetUbuntuVersionInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;
        
        if (IsLinux && distro == IdentifiedLinuxDistro.Ubuntu)
        {
            result = new OsVersionInfo();

            do
            {
                #region | Get the os version, codename and product name |
                
                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxOsReleaseDetailsArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, '=');
                
                if (!(infoDictionary?.Any() ?? false)) { break; }

                if (infoDictionary.TryGetValue("VERSION", out var version)
                    && (!string.IsNullOrWhiteSpace(version)))
                {
                    result.VersionNumber = version.Trim();

                    var versionTextParts = version.Trim()
                        .Split(" ")
                        .Where(w => !string.IsNullOrWhiteSpace(w))
                        .Select(s => s.Trim())
                        .ToArray();

                    result.IsLongTermSupported =
                        versionTextParts.Any(a => a.Equals("LTS", StringComparison.InvariantCultureIgnoreCase));
                    
                    var versionParts = GetVersionParts(versionTextParts[0]);
                    result.MajorVersion = versionParts.MajorVersion;
                    result.MinorVersion = versionParts.MinorVersion;
                    result.BuildVersion = versionParts.BuildVersion;
                    result.RevisionVersion = versionParts.RevisionVersion;
                }
                
                if (infoDictionary.TryGetValue("VERSION_CODENAME", out var codename)
                    && (!string.IsNullOrWhiteSpace(codename)))
                {
                    result.VersionCodename = codename.Trim();
                }

                if (infoDictionary.TryGetValue("NAME", out var name)
                    && (!string.IsNullOrWhiteSpace(name)))
                {
                    result.ProductName = name.Trim();
                }

                if (infoDictionary.TryGetValue("PRETTY_NAME", out var displayName)
                    && (!string.IsNullOrWhiteSpace(displayName)))
                {
                    result.ProductNameDisplay = displayName.Trim();
                }

                #endregion
                
                #region | Get the BasedOnVersion info |
                
                shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, DebianVersionArgs, 
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                result.BasedOnVersion = $"Debian {shellResult.OutputLines[0].Trim()}";

                #endregion
            } while (false);
        }

        return result;
    }
    
    private async Task<OsVersionInfo> GetMintVersionInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;
        
        if (IsLinux && distro is IdentifiedLinuxDistro.Mint or IdentifiedLinuxDistro.LMDE)
        {
            result = new OsVersionInfo();

            do
            {
                #region | Get the os version, codename and product name |
                
                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxOsReleaseDetailsArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, '=');
                
                if (!(infoDictionary?.Any() ?? false)) { break; }

                if (infoDictionary.TryGetValue("VERSION", out var version)
                    && (!string.IsNullOrWhiteSpace(version)))
                {
                    result.VersionNumber = version.Trim();

                    var versionTextParts = version.Trim()
                        .Split(" ")
                        .Where(w => !string.IsNullOrWhiteSpace(w))
                        .Select(s => s.Trim())
                        .ToArray();

                    result.IsLongTermSupported =
                        versionTextParts.Any(a => a.Equals("LTS", StringComparison.InvariantCultureIgnoreCase));
                    
                    var versionParts = GetVersionParts(versionTextParts[0]);
                    result.MajorVersion = versionParts.MajorVersion;
                    result.MinorVersion = versionParts.MinorVersion;
                    result.BuildVersion = versionParts.BuildVersion;
                    result.RevisionVersion = versionParts.RevisionVersion;
                }
                
                if (infoDictionary.TryGetValue("VERSION_CODENAME", out var codename)
                    && (!string.IsNullOrWhiteSpace(codename)))
                {
                    result.VersionCodename = codename.Trim();
                }

                if (infoDictionary.TryGetValue("NAME", out var name)
                    && (!string.IsNullOrWhiteSpace(name)))
                {
                    result.ProductName = name.Trim();
                }

                if (infoDictionary.TryGetValue("PRETTY_NAME", out var displayName)
                    && (!string.IsNullOrWhiteSpace(displayName)))
                {
                    result.ProductNameDisplay = displayName.Trim();
                }

                #endregion
                
                #region | Get the BasedOnVersion info |
                
                shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, DebianVersionArgs, 
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                result.BasedOnVersion = $"Debian {shellResult.OutputLines[0].Trim()}";

                #endregion
            } while (false);
        }

        return result;
    }
        
    private async Task<OsVersionInfo> GetWindowsVersionInfo(bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;

        if (IsWindows)
        {
            result = new OsVersionInfo();

            var version = Environment.OSVersion.Version;

            if (withConsoleOutput)
            {
                Console.WriteLine("Windows version info -");
                Console.WriteLine($"Major: {version.Major}");
                Console.WriteLine($"Minor: {version.Minor}");
                Console.WriteLine($"Build: {version.Build}");
                Console.WriteLine($"Revision: {version.Revision}");
            }
            
            result.VersionNumber = version.ToString();
            result.MajorVersion = version.Major;
            result.MinorVersion = version.Minor;
            result.BuildVersion = version.Build;
            result.RevisionVersion = version.Revision;

            var winVersion = result.MajorVersion;
            
            //refer to: https://stackoverflow.com/questions/69038560/detect-windows-11-with-net-framework-or-windows-api
            if (winVersion == 10 && result.MinorVersion >= 0 && result.BuildVersion >= 22000)
            {
                winVersion = 11;
            }

            result.ProductName = $"Microsoft Windows {winVersion}";

            //Satisfy the compiler that something async is happening
            var details = await Task.FromResult(string.Empty);

            if (!string.IsNullOrWhiteSpace(Environment.OSVersion.ServicePack))
            {
                details += $"Service Pack {Environment.OSVersion.ServicePack}";
            }

            details += $"{((!string.IsNullOrWhiteSpace(details)) ? " - " : "")}{Environment.OSVersion.Platform}";

            result.ProductNameDisplay = $"{result.ProductName} ({details.Trim()})";
        }

        return result;
    }

    private async Task GetWindowsUserInfo(bool withConsoleOutput = false)
    {
        if (IsWindows)
        {
#pragma warning disable CA1416
            using var identity = WindowsIdentity.GetCurrent();

            if (identity != null)
            {
                if (withConsoleOutput)
                {
                    Console.WriteLine("Windows identity found -");
                    Console.WriteLine($"Name: {identity.Name}");
                    var claims = identity.Claims?.ToArray();
                    if (claims?.Any() ?? false)
                    {
                        Console.WriteLine("User claims:");
                        foreach (var claim in claims)
                        {
                            Console.WriteLine($" - {claim}");
                        }
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(identity.Name))
                {
                    RunningAsUser = identity.Name.Trim();
                }

                IsAdminUser = (new WindowsPrincipal(identity)).IsInRole(WindowsBuiltInRole.Administrator);
            }
#pragma warning restore CA1416

            if (string.IsNullOrWhiteSpace(RunningAsUser))
            {
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("USERNAME")))
                {
                    RunningAsUser = Environment.GetEnvironmentVariable("USERNAME")!.Trim();
                }
                else
                {
                    //Satisfy the compiler that something async is happening
                    RunningAsUser = await Task.FromResult(string.Empty);
                }
            }
        }
    }

    private async Task<OsVersionInfo> GetMacOsVersionInfo(bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;

        if (IsMacOs)
        {
            result = new OsVersionInfo();

            do
            {
                var shellResult = await RunUnixShellCommand(MacOsInfoCommand, MacOsInfoCommandArgs, 
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, ':');
                
                if (infoDictionary.TryGetValue("System Version", out var sysVersion)
                    && (!string.IsNullOrWhiteSpace(sysVersion)))
                {
                    var sysVersionParts = sysVersion.Split(' ')
                        .Where(w => !string.IsNullOrWhiteSpace(w))
                        .Select(s => s.Trim())
                        .ToArray();

                    foreach (var part in sysVersionParts)
                    {
                        var versionParts = GetVersionParts(part);
                        if (versionParts is { MajorVersion: > 0, MinorVersion: not null })
                        {
                            result.MajorVersion = versionParts.MajorVersion;
                            result.MinorVersion = versionParts.MinorVersion;
                            result.BuildVersion = versionParts.BuildVersion;
                            result.RevisionVersion = versionParts.RevisionVersion;
                            
                            result.VersionNumber = $"macOS {part.Trim()}";
                            
                            break;
                        }
                    }
                }                
                
                if (infoDictionary.TryGetValue("Kernel Version", out var basedOn)
                    && (!string.IsNullOrWhiteSpace(basedOn)))
                {
                    result.BasedOnVersion = basedOn.Trim();

                    var darwinVersion =
                        GetVersionParts(basedOn.Replace("darwin", "", StringComparison.InvariantCultureIgnoreCase));
                    if (darwinVersion.MajorVersion > 0)
                    {
                        if (MacOsCodenames.Any(a => a.DarwinVersion == darwinVersion.MajorVersion))
                        {
                            result.VersionCodename = MacOsCodenames
                                .First(f => f.DarwinVersion == darwinVersion.MajorVersion)
                                .Codename;
                        }
                        else
                        {
                            var codename = MacOsCodenames.MaxBy(o => o.DarwinVersion);
                            if (darwinVersion.MajorVersion > codename.DarwinVersion)
                            {
                                result.VersionCodename = $"newer than {codename.Codename}";
                            }
                        }
                    }
                }
            } while (false);
        }

        return result;
    }
    
    private async Task GetMacOsUserInfo(bool withConsoleOutput = false)
    {
        if (IsMacOs)
        {
            var shellResult = await RunUnixShellCommand(UnixUsernameCommand, 
                showOutput: withConsoleOutput);
            if (shellResult.IsComplete && (!shellResult.IsError) && (!shellResult.IsEmptyOutput))
            {
                RunningAsUser = shellResult.OutputLines[0].Trim();
                IsAdminUser = IsUnixRootUser(RunningAsUser);
            }
        }
    }

    private OsVersionInfo GetAndroidVersionInfo()
    {
        var result = new OsVersionInfo
        {
            VersionNumber = null,
            MajorVersion = 0,
            MinorVersion = null,
            BuildVersion = null,
            RevisionVersion = null,
            IsLongTermSupported = null,
            VersionCodename = null,
            BasedOnVersion = null,
            ProductName = null,
            ProductNameDisplay = null
        };

#if (MAUI || HAS_UNO)
        var version = string.Empty;
        var major = 0;
        var minor = 0;
        var build = 0;
        var model = string.Empty;
        var manufacturer = string.Empty;

#if MAUI
        var info = DeviceInfo.Current.Version;

        if (info.Major > 0)
        {
            result.MajorVersion = major = info.Major;
            version += $"{major}";

            if (info.Minor > -1)
            {
                result.MinorVersion = minor = info.Minor;
                version += $".{minor}";

                if (info.Build > -1)
                {
                    result.BuildVersion = build = info.Build;
                    version += $".{build}";

                    if (info.Revision > -1)
                    {
                        result.RevisionVersion = info.Revision;
                        version += $".{info.Revision}";
                    }
                }
            }
        }
        model = DeviceInfo.Current.Model?.Trim() ?? string.Empty;
        manufacturer = DeviceInfo.Current.Manufacturer?.Trim() ?? string.Empty;

#elif HAS_UNO
        try
        {
            var encoded = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            var v = ulong.Parse(encoded);
            major = (int)(ushort)((v & 0xFFFF000000000000L) >> 48);
            minor = (int)(ushort)((v & 0x0000FFFF00000000L) >> 32);
            build = (int)(ushort)((v & 0x00000000FFFF0000L) >> 16);
            var revision = (int)(ushort)((v & 0x000000000000FFFFL));

            if (major > 0)
            {
                result.MajorVersion = major;
                result.MinorVersion = minor;
                version += $"{major}.{minor}";

                if (build > 0 || revision > 0)
                {
                    result.BuildVersion = build;
                    version += $".{build}";

                    if (revision > 0)
                    {
                        result.RevisionVersion = revision;
                        version += $".{revision}";
                    }
                }
            }
        }
        catch (Exception)
        {
            //Version info could not be decoded
        }
#endif

        if (major > 0)
        {
            foreach (var kvp in AndroidCodenames.OrderByDescending(o => o.Key))
            {
                if (major >= kvp.Value.Version.Major
                    && minor >= kvp.Value.Version.Minor
                    && build >= kvp.Value.Version.Build)
                {
                    result.VersionCodename = kvp.Value.Codename;
                    result.ProductName = $"Android {version} ({kvp.Value.Codename}) API level {kvp.Key}";
                    break;
                }
            }

            result.VersionNumber = version;
            if (string.IsNullOrWhiteSpace(result.ProductName))
            {
                result.ProductName = $"Android {version}";
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                result.ProductNameDisplay = result.ProductName;
            }
            else
            {
                result.ProductNameDisplay = $"{result.ProductName} - {model}"
                                            + $"{(string.IsNullOrWhiteSpace(manufacturer) ? string.Empty : $" ({manufacturer})")}";
            }
        }

#endif

        return result;
    }

    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public bool IsMacOs => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

#if MAUI
    public bool IsAndroid => DeviceInfo.Current.Platform == DevicePlatform.Android;
#elif (WIN_UI || HAS_UNO)
    public bool IsAndroid => (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily?.Trim() ?? string.Empty)
        .StartsWith("Android.", StringComparison.InvariantCultureIgnoreCase);
#else
    public bool IsAndroid => false;
#endif

    public bool IsX64 => RuntimeInformation.OSArchitecture == Architecture.X64;
    public bool IsArm64 => RuntimeInformation.OSArchitecture == Architecture.Arm64;

    public string OsDescription => RuntimeInformation.OSDescription;
    
    public IdentifiedLinuxDistro LinuxDistro { get; private set; } = IdentifiedLinuxDistro.Unknown;
    public OsVersionInfo OsVersionInfo { get; private set; }
    
    public string RunningAsUser { get; private set; }
    public bool? IsAdminUser { get; private set; }

    public string OsVersion => (string.IsNullOrWhiteSpace(OsVersionInfo?.FullVersion))
        ? "(information not available)"
        : OsVersionInfo.FullVersion;

    public string ProductName => (!string.IsNullOrWhiteSpace(OsVersionInfo?.ProductName))
        ? OsVersionInfo.ProductName.Trim()
        : (!string.IsNullOrWhiteSpace(OsDescription))
            ? OsDescription
            : string.Empty;

    public string ProductNameDisplay => (!string.IsNullOrWhiteSpace(OsVersionInfo?.ProductNameDisplay))
        ? OsVersionInfo.ProductNameDisplay.Trim()
        : (!string.IsNullOrWhiteSpace(OsDescription))
            ? OsDescription.Trim()
            : (!string.IsNullOrWhiteSpace(OsVersionInfo?.ProductName))
                ? OsVersionInfo.ProductName.Trim()
                : string.Empty;
    
    public string LinuxDistroName => (IsLinux)
        ? LinuxDistro.ToString()
        : string.Empty;

    public string PlatformOsName => (IsWindows)
        ? "Microsoft Windows"
        : (IsMacOs)
            ? "Apple macOS"
            : (IsAndroid)
                ? "Android"
                : (IsLinux)
                    ? $"Linux ({((LinuxDistro == IdentifiedLinuxDistro.Unknown) ? "unidentified distro" :  LinuxDistro.ToString())})"
                    : "Unknown";

    public string DotNetVersion => RuntimeEnvironment.GetSystemVersion();

    public string PlatformArchitecture => RuntimeInformation.OSArchitecture.ToString();

    public async Task<SimpleOsInfo> Gather(bool withConsoleOutput = false)
    {
        if (IsMacOs)
        {
            LinuxDistro = IdentifiedLinuxDistro.NotLinux;
            OsVersionInfo = await GetMacOsVersionInfo(withConsoleOutput);
            await GetMacOsUserInfo(withConsoleOutput);
        }
        else if (IsWindows)
        {
            LinuxDistro = IdentifiedLinuxDistro.NotLinux;
            OsVersionInfo = await GetWindowsVersionInfo(withConsoleOutput);
            await GetWindowsUserInfo(withConsoleOutput);
        }
        else if (IsAndroid)
        {
            LinuxDistro = IdentifiedLinuxDistro.Android;
            OsVersionInfo = GetAndroidVersionInfo();
            RunningAsUser = "mobile";
            IsAdminUser = false;
        }
        else if (IsLinux)
        {
            do
            {
                //Step 1 - try to identify the distro
                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxIdentifyDistroArgs,
                    showOutput: withConsoleOutput);
                if (!shellResult.IsError)
                {
                    LinuxDistro = IdentifyLinuxDistro(shellResult.OutputLines[0]);
                }

                if (withConsoleOutput)
                {
                    Console.WriteLine((shellResult.IsError)
                        ? $"Unable to get Linux distro information, because of error: {shellResult.Error}"
                        : $"Linux distro information: {shellResult.OutputLines[0]}"
                          + $" - Identified distro: {LinuxDistro}");
                }

                if (shellResult.IsError || LinuxDistro == IdentifiedLinuxDistro.Unknown)
                {
                    break;
                }

                //Step 2 - try to get the version info (including exact version #) plus product and user
                switch (LinuxDistro)
                {
                    case IdentifiedLinuxDistro.Alpine:
                        //TODO:
                        break;
                    
                    case IdentifiedLinuxDistro.Debian:
                        OsVersionInfo = await GetDebianVersionInfo(LinuxDistro, withConsoleOutput);
                        await GetDebianUserInfo(LinuxDistro, withConsoleOutput);
                        break;
                    
                    case IdentifiedLinuxDistro.Ubuntu:
                        OsVersionInfo = await GetUbuntuVersionInfo(LinuxDistro, withConsoleOutput);
                        await GetDebianUserInfo(LinuxDistro, withConsoleOutput);
                        break;
                    
                    case IdentifiedLinuxDistro.Mint:
                    case IdentifiedLinuxDistro.LMDE:
                        OsVersionInfo = await GetMintVersionInfo(LinuxDistro, withConsoleOutput);
                        await GetDebianUserInfo(LinuxDistro, withConsoleOutput);
                        break;
                }

            } while (false);
        }

        return this;
    }

    public bool IsUnixRootUser(string username) =>
        (!string.IsNullOrWhiteSpace(username))
        && username.Trim().Equals(UnixRootUsername, StringComparison.InvariantCultureIgnoreCase);

    public IdentifiedLinuxDistro IdentifyLinuxDistro(string distroInfo)
    {
        var result = IdentifiedLinuxDistro.Unknown;

        if (IsLinux && (!string.IsNullOrWhiteSpace(distroInfo)))
        {
            var distro = distroInfo.Trim().ToLowerInvariant();
            if (distro.StartsWith(DebianDistroIdentifier))
            {
                result = IdentifiedLinuxDistro.Debian;
            }
            else if (distro.StartsWith(LmdeDistroIdentifier)
                     || distro.StartsWith($"linux {MintDistroIdentifier} {DebianDistroIdentifier}"))
            {
                result = IdentifiedLinuxDistro.LMDE;
            }
            else if (distro.StartsWith(MintDistroIdentifier)
                     || distro.StartsWith($"linux {MintDistroIdentifier}"))
            {
                result = IdentifiedLinuxDistro.Mint;
            }
            else if (distro.StartsWith(UbuntuDistroIdentifier))
            {
                result = IdentifiedLinuxDistro.Ubuntu;
            }
            else if (distro.Contains(AlpineDistroIdentifier))
            {
                result = IdentifiedLinuxDistro.Alpine;
            }
        }

        return result;
    }

    public async Task<RunUnixShellCommandResult> RunUnixShellCommand(
        string command,
        string args = null,
        bool ignoreWarnings = true,
        bool showOutput = true,
        int? postRunWaitSeconds = null
    )
    {
        var result = new RunUnixShellCommandResult(isComplete: false);

        if (string.IsNullOrWhiteSpace(command))
        {
            result.Exception = new ArgumentException("Value cannot be null or blank.", nameof(command));
            result.Error = $"Invalid command - {result.Exception.Message}";
        }
        else
        {
            var tcs = new TaskCompletionSource<RunUnixShellCommandResult>();

            var shellTask = async () =>
            {
                var taskResult = new RunUnixShellCommandResult(isComplete: false);

                try
                {
                    if (showOutput)
                    {
                        Console.WriteLine($"\nAttempting to run the shell command: {command}"
                            + $"{((string.IsNullOrWhiteSpace(args)) ? "" : $" {args}")}");
                    }

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = command.Trim(),
                            Arguments = (args ?? string.Empty).Trim(),
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();  //TODO: Can't use this line on iOS; will have to find an alternative, or just make code unavailable

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var doDone = false;
                    
                    if (showOutput && (!string.IsNullOrWhiteSpace(output)))
                    {
                        var lines = output.Split(UnixLineSplitters, StringSplitOptions.RemoveEmptyEntries);
                        Console.WriteLine("====OUTPUT====");
                        foreach (var line in lines)
                        {
                            Console.WriteLine(line);
                        }
                        doDone = true;
                    }

                    if (showOutput && (!string.IsNullOrWhiteSpace(error)))
                    {
                        var lines = error.Split(UnixLineSplitters, StringSplitOptions.RemoveEmptyEntries);
                        Console.WriteLine("====ERROR/WARNING INFO====");
                        var removeWarnings = new List<string>();
                        foreach (var line in lines)
                        {
                            Console.WriteLine(line);
                            if (ignoreWarnings 
                                && (!line.Trim().StartsWith("warning", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                removeWarnings.Add(line);
                            }
                        }
                        doDone = true;

                        if (ignoreWarnings)
                        {
                            error = (removeWarnings.Any())
                                ? string.Join('\n', removeWarnings)
                                : string.Empty;
                        }
                    }

                    if (doDone)
                    {
                        Console.WriteLine("====DONE====");
                    }
                    
                    // ReSharper disable once ConstantNullCoalescingCondition
                    taskResult.Output = output ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(error))
                    {
                        if (postRunWaitSeconds is > 0)
                        {
                            if (showOutput)
                            {
                                Console.WriteLine($"Waiting {postRunWaitSeconds.Value} seconds for operation cleanup...");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(postRunWaitSeconds.Value));
                        }
                        taskResult.SetComplete();
                    }
                    else
                    {
                        taskResult.Output = error;
                    }
                }
                catch (Exception e)
                {
                    taskResult.Exception = e;
                    taskResult.Error = (!string.IsNullOrWhiteSpace(e.Message))
                        ? e.Message.Trim()
                        : "An unexpected error occurred.";

                    if (showOutput)
                    {
                        Console.WriteLine("====SHELL PROCESS EXCEPTION INFO====");
                        Console.WriteLine(e.ToString());
                        Console.WriteLine("====DONE====");
                    }
                }

                tcs?.TrySetResult(taskResult);
            };

            _ = shellTask.Invoke();

            result = await tcs.Task;
        }

        return result;
    }
}

#endregion
