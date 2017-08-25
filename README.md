# Navigating Time in C#

> One of Tlön's schools manages to refute time, reasoning that the present is indeterminate, that the future has no reality except as present hope, and that the past has no reality except as present memory. Another school claims that all time has already passed and that our lives are barely the memory or dim reflection, doubtless falsified and distorted, of an irrecoverable process. Another, that the history of the world - and in it our lives and every least detail of our lives - is the scripture produced by a lesser god to communicate with a demon. Another, that the world is comparable to those codes in which some symbols have no meaning and the only truth is what takes place every three hundred nights.

  -- Jorge Luis Borges, _Tlön, Uqbar, Orbis Tertius_


> The whole universe of concrete objects, as we know them, swims…in a wider and higher universe of abstract ideas, that lend it its significance…time, space, and the ether soak through all things…form the background for all our facts, the fountain-head of all the possibilities we conceive of…We can never look directly at them, for they are bodiless and featureless and footless, but we grasp all other things by their means.

  -- William James, _The Varieties of Religious Experience_


## Introduction - The Closed Captions Editor

For the past few months, I’ve been working on a new piece of Articulate software: a Closed Captions Editor that allows users to create and modify captions for their audio and video media. This project allowed me to work on and think deeply about some of the multi-threading challenges that are characteristic of software that exposes a timeline, supports a playback mode and allow users to navigate forward and backward through time on this timeline.

![The Closed Captions Editor](https://github.com/Amichai/TimelineApp/blob/master/Images/CCE.png "The Closed Captions Editor")

_Disclaimer:_ I am not a C#-multi-threading expert. The "correctness" I target in the real-world and in this blog post is the "correctness" that passes the black-box testing I describe in the "verification" section of this post.
 
## Timeline Application First-Pass

At the heart of any "timeline application" is some component dedicated to driving playback and keeping track of the current time. During playback mode, this component volunteers the sequence of time values that cause the UI to update in a continuous progression of frames. We’ll call this component a “clock.” In its most simple C# iteration, we can imagine our clock built on a `System.Threading.Timer` timer and a `TimeSpan currentTime` field that gets progressed forward in time on each tick of the timer. For our clock to be more useful, it might also expose: `Play()`, `Pause()`, `Stop()` and `Seek()` methods and a `NewTimeValue` `EventHandler`. For this implementation, `Seek()` first pauses playback before seeking to the desired position on the timeline. (In the Closed Captions Editor, our clock component was also our media playback component.)

In C#, a simple version of this clock might be:

```
internal sealed class Clock
{
    public Clock()
    {
        timer = new Timer(state => Tick(), null, 0, 10);
    }

    public void Seek(TimeSpan time)
    {
        IsPlaying = false;
        CurrentTime = time;
        NotifyNewTimeValue();
    }

    private void Tick()
    {
        if (!IsPlaying)
        {
            lastUpdate = null;
            return;
        }

        if (CurrentTime == null)
        {
            CurrentTime = TimeSpan.Zero;
        }

        var diff = DateTime.Now - lastUpdate;
        CurrentTime += diff ?? TimeSpan.Zero;
        lastUpdate = DateTime.Now;
        NotifyNewTimeValue();
    }
}
```

Around this clock we can build a UI with a timeline and other components that change in concert with our clock’s current time value. The code-behind for that WPF UI might be:

```
public partial class MainWindow : Window, INotifyPropertyChanged
{
    ...

    private readonly Clock clock;

    public MainWindow()
    {
        InitializeComponent();

        clock = new Clock();
        clock.NewTimeValue += ClockOnNewTimeValue;
        clock.Play();
    }

    public void Seek()
    {
        var seekTime = TimeSpan.FromSeconds(1);
        clock.Seek(seekTime);
    }

    private void ClockOnNewTimeValue(object sender, NewTimeValueEventArg arg)
    {
        TimeValue = arg.Time;
    }
}
```

## What’s the Matter with Seek?

This code seems to work fine, and may even be stable for a long time into the future. However, as our UI gets more complex and our `NewTimeValue` callback starts to take on new responsibilities, our application may fail in subtle ways (multi-threading is hard).

We can simulate a more complex version of our application by adding a `Thread.Sleep()` in our `NewTimeValue()` callback:
```
private void ClockOnNewTimeValue(object sender, NewTimeValueEventArg arg)
{
    Thread.Sleep(80);
    TimeValue = arg.Time;
}
```

In this version of the code, `Seek()` no longer works reliably. We may be playing through the timeline in playback mode and when trying to seek to time A from our current time B, we still find ourselves at time B on the timeline. 
What’s going on here?

![Threading Context Diagram](https://github.com/Amichai/TimelineApp/blob/master/Images/Threads.png "Threading Context Diagram")
 
Simply put, the clock’s timer and the UI seek functionality are racing to set the current time value on the clock component from separate threads. As `NewTimeValue` becomes more complex, it becomes more likely that we see an old/spurious timer value come in to `ClockOnNewTimeValue()` after the clock’s value has been updated by the UI driven `Seek()` operation. This bug is just one version of a whole category of race-condition bugs that can emerge in this kind of architecture. Another example we encountered in the Closed Caption Editor: if a user clicks on a caption that is partially outside the currently visible timeline area, we want to scroll that caption into view, and that scroll operation is racing against a clock driven timeline scroll which occasionally result in all sorts of scrolling/visual nonsense.

To forestall all these potential issues, we will want to enforce the following requirement: after the user seeks on the timeline, the `NewTimeValue` callback shouldn’t process any time values that were initiated by the clock from before said seek operation. 

## The Most Natural Solution: Locking Over Current Time Updates

An intuitive solution that will occur to every C# developer is to lock over updates to the `Clock.currentTime` field and the `NewTimeValue` event. Locking in this way will guarantee the atomicity of all time updates so that `NewTimeValue` can never see a stale time value after having already processed a more recent `Seek()` value. Seek now looks like this:

```
public void Seek(TimeSpan time)
{
    lock (@lock)
    {
        IsPlaying = false;
        currentTime = time;
        NotifyNewTimeValue();
    }
}
```
Similarly for `Tick()`:
```
private void Tick()
{
    lock (@lock)
    {
        if (!IsPlaying)
        {
            lastUpdate = null;
            return;
        }

        if (currentTime == null)
        {
            currentTime = TimeSpan.Zero;
        }

        var diff = DateTime.Now - (lastUpdate ?? DateTime.Now);
        currentTime += diff;
        lastUpdate = DateTime.Now;
        NotifyNewTimeValue();
    }
}
```

This is an appealing fix and our application may work as expected for some time, but sadly it may also fail spectacularly...can you guess why? Hint: deadlock is not the answer here.

_Answer:_ `ClockOnNewTimeValue()` takes longer to compute than the period of our underlying threading timer. Additionally, our `clock.Tick()` invocations are happening from multiple threads in the thread-pool and each of these threads has to wait its turn to acquire the `@lock` lock in `Tick()`. This results in the accumulation of “backpressure” as threads are queued up to acquire the lock and this back-pressure can prevent the UI-driven `clock.Seek()` method from ever getting to the front of that queue, preventing the `Seek()` operation from progressing. This results in a blocked Dispatcher and a frozen UI. 

It goes without saying that this is a nightmare bug. We can’t expect to know how long any particular `ClockOnNewTimeValue` method execution will take as that may depend on any number of unique application-state conditions, and system characteristics which (in addition to the code) is liable to change at any point in the future.

Part of a solution is to require that all background `Tick()` operations happen on the same background thread. This is accomplished by moving away from the `Threading.Timer` and replacing it without our own managed thread that pumps out time values in a loop. 
```
public Clock()
{
    new TaskFactory().StartNew(() =>
    {
        while (true)
        {
            Tick();
            Thread.Sleep(10);
        }
    }, TaskCreationOptions.LongRunning);
}
```
It works!

## Deadlocks ☹

….until we try to do any UI related work inside our `ClockOnNewTimeValue()` method. Now we’re exposed to a nasty deadlock.

Our breaking scenario: the user clicks on the UI to perform the `Seek()` operation from the dispatcher thread, and is blocked on clock’s `@lock` lock. Simultaneously, the `@lock` has been acquired by a `Tick()` invocation which is blocked on the dispatcher thread trying to perform a `Dispatcher.Invoke()` operation inside `ClockOnNewTimeValue()`.

One possible solution for our deadlock problem is to replace any `Dispatcher.Invoke()` that can happen within a `@lock` lock with a `Dispatcher.BeginInvoke()`. Unfortunately, this kind of solution is delicate and tough to enforce. In a more complex version of this app it can expensive to move away from synchronous `Dispatcher.Invoke()`s that are used within property getters for example. In our Closed Caption Editor, the code for extracting html from rich text boxes on our timeline is dispatcher bound, so it would be an expensive and fraught refactor to eliminate the `Dispatcher.Invoke()` in that case.

Another possible solution that comes to mind: marshal all user-initiated events to the thread-pool by nesting calls in a `Task.Run()`. This is hardly an appealing solution. Pushing UI events to a background thread for no apparent reason is a maintainability nightmare and a code-smell and results in all sorts of obstacles around exception handling. 

Although appealing at first glance, another reason to be skeptical of this aforementioned locking solution: it requires locking over a raised event which is always risky and a potential code-smell.

## Some General Thoughts on Thread-Safety 

Deadlock bugs are terrible. A deadlock generally means: a) all unsaved application state is lost, b) the application won’t crash so no diagnostics or errors get reported, c) the user is stuck with a frozen application that they must kill from the task manager, d) we are dealing with a rare, non-deterministic, hard to reproduce and hard to diagnose bug that haunts the dreams of developers, testers and managers.

A central question that any developer needs to grapple with when writing multi-threaded code – "what does thread-safety mean to me?" At a basic level, “thread-safe” means that our application won’t crash because of a cross-thread access exception, and won’t deadlock. But “thread-safe” means something significantly different in the context of our timeline application. For us, “thread-safe” means enforcing an order in the `NewTimeValue` events that we process. For us, “thread-safe” means guaranteeing a coherence and consistency in our time processing logic and the contours of this consistency are unique to the application we happen to be working on the specific requirements we’re trying to meet.

In practice, we’re concerned about processing spurious and inconsistent time-values that can periodically break seeking or scrolling on the timeline, but rare seek and scrolling glitches are far more tolerable than equally rare crashes or deadlocks. In practice, we may be willing to exchange an occasionally glitchy seek for increased confidence that our application won’t ever deadlock. 

## Conceptualizing Time

> Broadly speaking, there are two major sources of date and time data: clocks, and the brains of users.
> 
> …
> 
> There are some areas of overlap, of course: a user can enter their date of birth, which is a coarse representation of a specific clock time. Or you may have someone manually logging events somewhere, with that log later being reconciled with system-generated events. These can sometimes lead to grey areas with no one "right" decision, but I would still tend to consider these as user data.

Source: http://nodatime.org/2.2.x/userguide/type-choices

So far, our investigation has left us with two questions: 1) how do we accomplish a timeline seek in a way that is both coherent, consistent and safe? 2) why is this problem that seems so simple, so surprisingly hard to solve? 

This problem is surprisingly hard to solve because we are using a single method and a single data-type for handling time values that come from user input (a seek operation) and time values that come from an underlying clock tick.

On the one hand, when a user seeks to point on the timeline, we expect the UI to reflect the same state that would result from traversing that same point on the timeline during timeline playback. However, from a philosophical perspective, these two sources of time values are completely distinct and they operate in profoundly different ways. For example, we expect clock time values being pumped in the context of a playback operation to be roughly continuous. User-generated time values on the other-hand are always expected to be discontinuous (for most applications, we don’t expect and don’t care to support a user trying to navigate through the timeline in 10 millisecond step sizes). Clock generated time-values are produced at a high frequency, and are subject to subtle back-pressure bugs and spurious/invalid value bugs – not so for user-generated time-values. In other words, to achieve thread safety in our context we need to enforce a sense of thread-safety that is rooted in the specific dynamics of user-time and clock-time as defined by the usages and requirements of our application.

Thread safety in the context of our time-driven application means that user-generated time values are always respected and given precedence over clock-generated values. Thread safety means we should never be dropping a user initiated time value, but we are cautiously skeptical of clock generated values and we don’t have a problem dropping some of those if we think there’s a chance they might be stale or spurious. Armed with these assumptions, we can achieve our desired sense of thread-safety by explicitly noting the source of the time value we’re seeing and handling it accordingly.

Two solutions of this category, each with their own risks and tradeoffs, can be considered:

_Solution #1:_

The `NewTimeValue` callback is modified to include a parameter that indicates where this time value came from. If the time value came from a user initiated seek or pause, we set a flag that tells us to suppress all clock initiated time values until we get a new clock value initiated by a user “play” operation.

_Solution #2:_

We can write a filter that sits on top of the `NewTimeValue` event and is solely responsible for maintaining the current time value. This `ClockSeekFilter` knows to always honor time values coming from a foreground thread, but has the liberty to ignore time values that come from a background thread if that time value is sufficiently surprising (discontinuous from the last assumed current time value). If however, we get three surprising clock values in a row, we will throw out our original assumption about the current time and defer to the new clock-driven assumption:

```
internal sealed class ClockSeekFilter
{
    public TimeSpan CurrentValue => currentTimeValue ?? TimeSpan.Zero;

    private int dropCount;
    private TimeSpan? currentTimeValue;
    private readonly TimeSpan continuityThreshold;
    private const int dropThreshold = 3;
    private readonly object @lock = new object();

    public ClockSeekFilter(TimeSpan continuityThreshold)
    {
        this.continuityThreshold = continuityThreshold;
    }

    public void Filter(TimeSpan newTimeValue, bool isBackgroundThread)
    {
        lock (@lock)
        {
            if (isBackgroundThread)
            {
                if (currentTimeValue == null)
                {
                    UpdateCurrentTime(newTimeValue);
                    return;
                }

                var diff = newTimeValue - currentTimeValue.Value;
                if (diff < continuityThreshold)
                {
                    UpdateCurrentTime(newTimeValue);
                }
                else if (++dropCount > dropThreshold)
                {
                    UpdateCurrentTime(newTimeValue);
                }
            }
            else
            {
                UpdateCurrentTime(newTimeValue);
            }
        }
    }

    private void UpdateCurrentTime(TimeSpan time)
    {
        currentTimeValue = time;
        dropCount = 0;
    }
}	
```

With Reactive Extensions, this `ClockSeekFilter` method allows us to achieve our goal of "thread safety" with a completely modular filter-component as follows:
```
public MainWindow()
{
    InitializeComponent();

    clock = new Clock();
    Observable.FromEventPattern<NewTimeValueEventArg>(handler => clock.NewTimeValue += handler, handler => clock.NewTimeValue -= handler)
      .Select(arguments => arguments.EventArgs)
      .Select(eventArg => continuityFilter.Filter(eventArg.Time, Thread.CurrentThread.IsBackground))
      .Subscribe(time => ProcessNewTimeValue());
    clock.Play();
}
```

_Notice:_

Notice we aren't returning `currentTimeValue` outside of `Filter` and passing that value to `ProcessNewTimeValue`. For `ClockSeekFilter` to be effectiev we can only allow for one source of "ground truth" and that source must be the `CurrentTimeValue` property in `ClockSeekFilter` whose value is only set under a class-local lock. If we were to return a `CurrentTimeValue` value outside of our filter, we would be leaking independent and potentially stale notions of the current-time into our application logic. Specifically, if `Filter()` returned a time value from the clock running on a background thread, and then immediately returned a different and updated time value from a UI interaction, we can make no guarantees about the order in which these two values would get processed in `ProcessNewTimeVaule()`, thereby reintroducing the original race-condition we were trying to fix.

## Verification

How can I be confident that the solutions I suggest here are effective? Is it because I’m a multi-threading expert? No. It’s because I wrote a test harness that exercises `PlayPause()` and `Seek()` in a tight loop and is simultaneously testing to see that the dispatcher can be acquired so we know we haven’t deadlocked. For each proposed solution, I let this test-harness run for some time to gain confidence that the application doesn't crash or deadlock. 

`BlackBoxTester`:


```
public BlackBoxTester(MainWindow window)
{
    this.window = window;
}

public void Start()
{
    t1 = new Timer(state =>
    {
        window.Dispatcher.Invoke(() =>
        {
            window.Seek();
            Thread.Sleep(10);
            window.PlayPause();
        });
    }, null, 0, 50);

    t2 = new Timer(state =>
    {
        var task = Task.Run(() => TestForDeadlock());
        if (!task.Wait(TimeSpan.FromSeconds(1)))
        {
            Environment.FailFast("Deadlock");
        }
    }, null, 0, 50);
}

private void TestForDeadlock()
{
    window.Dispatcher.Invoke(() =>
    {
        count++;
    });
}
```

When dealing with complex multi-threading applications, this kind of black-box testing is generally the only reliable way to gain confidence in the correctness of one's code.


## Conclusion

Philosophers and scientists have long debated how to understand time. At the heart of this debate is a dichotomy between a conception of time as an absolute frame-of-reference that encompasses and animates all of nature on the one hand, and a conception of time that allows for an abundance of contradictory times and timelines and independent notions of temporality. 

Einstein’s Theory of Relativity revolutionized classical notions about the immutability of time by describing time as a dimension that could be warped and distorted and could pass at different speeds for different observers. In his [_New Refutation of Time_](http://heavysideindustries.com/wp-content/uploads/2010/10/borges-a-new-refutation-of-time.pdf "A New Refutation of Time"), Borges makes the more extreme claim that time is the construction of subjective consciousness and that time has no reality outside of subjective human and psychological experience.

This dichotomy between time as an absolute framework of perfectly sorted successions, and time as a localized and individuated notion that allows for contradiction, subjectivity and indeterminacy is a dichotomy to be negotiated when architecting clock-driven software. As developers, we can choose to enforce a single, sequential timeline of instants that drives the progress of our application in lock-step and whose manipulations and effects are all atomized. However, this construction of a single “ground-truth” notion of time is not without its costs. Alternatively, we can embrace an abundance of times, a polyphony of moments, and a variety of perspectives on temporality and negotiate questions of order and coherence “down-stream.”

> Our destiny…is not frightful by being unreal; it is frightful because it is irreversible and iron-clad. Time is the substance I am made of. Time is a river which sweeps me along, but I am the river; it is a tiger which destroys me, but I am the tiger; it is a fire which consumes me, but I am the fire. The world, unfortunately, is real; I, unfortunately, am Borges.

  -- Jorge Luis Borges, _A New Refutation of Time_