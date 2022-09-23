using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DesigTimeBuildRunner;

public class MySimpleLogger : Logger
{
    private List<object> _events = new ();

    public ILookup<Type, object> GroupedEvents => _events.ToLookup(x => x.GetType(), x=> x);
    
    public override void Initialize(Microsoft.Build.Framework.IEventSource eventSource)
    {
        //Register for the ProjectStarted, TargetStarted, and ProjectFinished events
        /*
        eventSource.ProjectStarted += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.ProjectFinished += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.ErrorRaised += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.WarningRaised += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.BuildStarted += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.BuildFinished += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.MessageRaised += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.TargetFinished += (sender, args) => {lock (_events) {_events.Add(args);}};
        eventSource.TargetStarted += (sender, args) => {lock (_events) {_events.Add(args);}};
        */
        eventSource.AnyEventRaised += (sender, args) => {lock (_events) {_events.Add(args);}};
    }

}