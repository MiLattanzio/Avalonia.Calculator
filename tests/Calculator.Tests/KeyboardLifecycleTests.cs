using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using AvaloniaCalculator.Views;
using UTDotNet.Avalonia;
using UTDotNet.Diagnostics;
using UTDotNet.UbuntuTouch;

internal static class KeyboardLifecycleTests
{
    public static void Run(Action<string,Action> check)
    {
        check("keyboard: outside tap restores currency layout without hidden callback",()=> {
            using var f=new Fixture();f.Window.SelectMode(16);Pump();
            var height=f.Find<Grid>("CurrencyKeypad").Bounds.Height;
            var session=f.OpenRate();f.Area(session);f.Rate.Text="1.25";
            Assert(f.Root.Padding.Bottom>0,"keyboard inset was not applied");
            f.Window.MouseDown(new Point(200,20),MouseButton.Left);f.Window.MouseUp(new Point(200,20),MouseButton.Left);Pump();
            Assert(!f.RateFlyout.IsOpen,"outside tap did not close flyout");f.Restored();
            Assert(session.HideRequests==1 && session.Disposed,"native input must hide before disposal");
            Assert(f.Find<Grid>("CurrencyKeypad").Bounds.Height==height,"currency keypad did not regain its height");
            Assert(f.Rate.Text=="1.25","manual rate was lost");
            f.Area(session);f.Restored(); // A delayed old callback must not shrink the page again.
        });
        check("keyboard: Escape closes rate and clears inset",()=> {
            using var f=new Fixture();var session=f.OpenRate();f.Area(session);
            f.Window.KeyPressQwerty(PhysicalKey.Escape,RawInputModifiers.None);
            f.Window.KeyReleaseQwerty(PhysicalKey.Escape,RawInputModifiers.None);Pump();
            Assert(!f.RateFlyout.IsOpen,"Escape did not close flyout");f.Restored();
            Assert(session.HideRequests==1,"Escape did not dismiss native input");
        });
        check("keyboard: reopen waits for old native hide and rejects stale callbacks",()=> {
            using var f=new Fixture();var session=f.OpenRate();f.Area(session);
            var hide=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);session.HideCompletion=hide.Task;
            f.RateFlyout.Hide();f.Restored();
            f.RateFlyout.ShowAt(f.Find<Button>("ManualCurrency"));f.Rate.Focus();Pump();
            Assert(f.Sessions.Count==1,"new session opened before old hide completed");
            hide.SetResult();Until(()=>f.Sessions.Count==2);
            var current=f.Sessions[^1];f.Area(current);var padding=f.Root.Padding;
            f.Send(session,new InputHidden());Assert(f.Root.Padding==padding,"old hide cleared new keyboard inset");
            f.Send(current,new CommitText("2",0,0,-1));Assert(f.Rate.Text!.Contains('2'),"reopened input did not accept text");
            f.RateFlyout.Hide();f.Restored();
        });
        check("keyboard: navigation from manual rate restores all 17 modes",()=> {
            using var f=new Fixture();
            for(int mode=0;mode<17;mode++)
            {
                var session=f.OpenRate();f.Area(session);f.Window.SelectMode(mode);Pump();f.Restored();
                Assert(!f.RateFlyout.IsOpen,"rate flyout survived navigation");
                f.Area(session);f.Restored();
            }
        });
        check("keyboard: date editor focus transfer retains inset and text",()=> {
            using var f=new Fixture();f.DateInputs();
            foreach(var name in new[]{"DateYears","DateMonths","DateDays"})
            {
                var field=f.Find<TextBox>(name);field.Focus();Pump();f.Area(f.Sessions[^1]);
                f.Send(f.Sessions[^1],new CommitText("2",0,0,-1));Pump();
                Assert(f.Root.Padding.Bottom>0,"focus transfer dismissed active editor inset");
                Assert(field.Text!.Contains('2'),"date field lost input");
            }
            f.Find<ComboBox>("DateMode").SelectedIndex=0;Pump();f.Restored();
            Assert(f.Sessions[^1].HideRequests==1,"hiding date inputs did not dismiss native input");
        });
        check("keyboard: every date field releases input on mode change",()=> {
            using var f=new Fixture();
            foreach(var name in new[]{"DateYears","DateMonths","DateDays"})
            {
                f.DateInputs();f.Find<TextBox>(name).Focus();Pump();var session=f.Sessions[^1];f.Area(session);
                f.Window.SelectMode(0);Pump();f.Restored();
                Assert(session.HideRequests==1 && session.Disposed,"navigation left a date context alive");
            }
        });
        check("keyboard: hidden or detached editor cannot retain an inset",()=> {
            using var f=new Fixture();f.DateInputs();var field=f.Find<TextBox>("DateYears");
            field.Focus();Pump();var session=f.Sessions[^1];f.Area(session);
            var parent=(Grid)field.Parent!;parent.IsVisible=false;f.Window.UpdateLayout();Pump();f.Restored();
            Assert(session.HideRequests==1,"hidden editor did not dismiss native input");
            parent.IsVisible=true;f.Window.UpdateLayout();field.Focus();Pump();session=f.Sessions[^1];f.Area(session);
            parent.Children.Remove(field);Pump();f.Restored();
            Assert(session.HideRequests==1,"detached editor did not dismiss native input");
        });
        check("keyboard: server hide, disconnect and window deactivation restore layout",()=> {
            using var f=new Fixture();f.DateInputs();var field=f.Find<TextBox>("DateDays");field.Focus();Pump();
            var session=f.Sessions[^1];f.Area(session);f.Send(session,new InputHidden());f.Restored();
            f.Area(session);f.Send(session,new InputDisconnected());f.Restored();
            f.Find<TextBox>("DateYears").Focus();Pump();session=f.Sessions[^1];f.Area(session);
            var other=new Window();try { other.Show();other.Activate();Pump();f.Restored();Assert(session.HideRequests==1,"deactivation did not dismiss keyboard"); }
            finally { other.Close(); }
        });
        check("keyboard: resize and clipboard menu do not leave stale padding",()=> {
            using var f=new Fixture();f.DateInputs();var field=f.Find<TextBox>("DateDays");field.Focus();Pump();f.Area(f.Sessions[^1]);
            field.SelectAll();
            if(field.ContextFlyout is {} menu) { menu.ShowAt(field);Pump();menu.Hide();field.Focus();Pump(); }
            f.Area(f.Sessions[^1]);Assert(f.Root.Padding.Bottom>0,"clipboard menu broke editor inset");
            f.Window.Width=740;f.Window.Height=360;f.Window.UpdateLayout();Pump();f.Area(f.Sessions[^1],160);
            Assert(f.Root.Padding.Bottom==160,"rotated viewport inset is wrong");
            f.Window.SelectMode(4);Pump();f.Restored();
            f.Window.Width=408;f.Window.Height=865;f.Window.UpdateLayout();Pump();f.Restored();
        });
    }
    private static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
    private static void Pump() => Dispatcher.UIThread.RunJobs();
    private static void Until(Func<bool> condition)
    {
        var timeout=DateTime.UtcNow.AddSeconds(3);
        while(!condition() && DateTime.UtcNow<timeout) { Pump();Thread.Sleep(1); }
        Pump();Assert(condition(),"timed out waiting for input lifecycle");
    }
    private sealed class Fixture : IDisposable
    {
        public MainWindow Window { get; }
        public List<FakeSession> Sessions { get; }=[];
        public Border Root=>Find<Border>("ShellRoot");
        public Flyout RateFlyout=>(Flyout)Find<Button>("ManualCurrency").Flyout!;
        public TextBox Rate=>((Control)RateFlyout.Content!).GetLogicalDescendants().OfType<TextBox>().Single();
        public Fixture()
        {
            var bridge=new MaliitInputBridge(new DiagnosticLog(),()=> { var session=new FakeSession();Sessions.Add(session);return session; });
            Window=new MainWindow(null,bridge) { Width=408,Height=865 };
            Window.Show();Window.Activate();Pump();
        }
        public T Find<T>(string name) where T:Control => Window.GetLogicalDescendants().OfType<T>().First(c=>c.Name==name);
        public FakeSession OpenRate()
        {
            Window.SelectMode(16);Pump();RateFlyout.ShowAt(Find<Button>("ManualCurrency"));Rate.Focus();Pump();
            Assert(Sessions.Count>0 && Sessions[^1].IsConnected,"rate input did not open a native session");return Sessions[^1];
        }
        public void DateInputs() { Window.SelectMode(3);Find<ComboBox>("DateMode").SelectedIndex=1;Pump(); }
        public void Area(FakeSession session,int height=240)
        {
            var origin=Window.PointToScreen(default);var size=PixelSize.FromSize(Window.ClientSize,Window.RenderScaling);
            Send(session,new InputAreaChanged(new InputArea(origin.X,origin.Y+size.Height-height,size.Width,height)));
        }
        public void Send(FakeSession session,MaliitEvent input)
        {
            var task=session.DispatchAsync!(input);Until(()=>task.IsCompleted);task.GetAwaiter().GetResult();Pump();
        }
        public void Restored() { Pump();Window.UpdateLayout();Assert(Root.Padding==default,"keyboard padding remained after editing ended: "+Root.Padding); }
        public void Dispose() { Window.Close();Pump(); }
    }
    private sealed class FakeSession : IMaliitSession
    {
        public Func<MaliitEvent,Task<MaliitWidgetState?>>? DispatchAsync { get; set; }
        public bool IsConnected { get; private set; }
        public bool Disposed { get; private set; }
        public int HideRequests { get; private set; }
        public Task HideCompletion { get; set; }=Task.CompletedTask;
        public Task OpenAsync(MaliitWidgetState state,CancellationToken token) { IsConnected=true;return Task.CompletedTask; }
        public Task UpdateAsync(MaliitWidgetState state,CancellationToken token)=>Task.CompletedTask;
        public Task ShowAsync(CancellationToken token)=>Task.CompletedTask;
        public Task HideAsync(CancellationToken token) { HideRequests++;return HideCompletion.WaitAsync(token); }
        public Task ResetAsync(CancellationToken token)=>Task.CompletedTask;
        public void Dispose() { Disposed=true;IsConnected=false; }
    }
}
