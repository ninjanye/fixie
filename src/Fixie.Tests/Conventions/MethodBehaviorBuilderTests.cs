﻿using System;
using System.Reflection;
using Fixie.Behaviors;
using Fixie.Conventions;
using Should;

namespace Fixie.Tests.Conventions
{
    public class MethodBehaviorBuilderTests
    {
        readonly MethodBehaviorBuilder builder;

        public MethodBehaviorBuilderTests()
        {
            builder = new MethodBehaviorBuilder();
        }

        public void ShouldJustInvokeMethodByDefault()
        {
            builder.Behavior.ShouldBeType<Invoke>();

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();
                
                builder.Behavior.Execute(Method("Pass"), this, exceptions);
                
                exceptions.Any().ShouldBeFalse();
                console.Lines.ShouldEqual("Pass");
            }

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Fail"), this, exceptions);

                exceptions.Count.ShouldEqual(1);
                console.Lines.ShouldEqual("Fail Threw!");
            }
        }

        public void ShouldAllowWrappingTheBehaviorInAnother()
        {
            builder.Wrap((method, instance, exceptions, inner) =>
            {
                Console.WriteLine("Before");
                inner.Execute(method, instance, exceptions);
                Console.WriteLine("After");
            });

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();
                
                builder.Behavior.Execute(Method("Pass"), this, exceptions);

                exceptions.Any().ShouldBeFalse();
                console.Lines.ShouldEqual("Before", "Pass", "After");
            }

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Fail"), this, exceptions);

                exceptions.Count.ShouldEqual(1);
                console.Lines.ShouldEqual("Before", "Fail Threw!", "After");
            }
        }

        public void ShouldAllowWrappingTheBehaviorMultipleTimes()
        {
            builder.Wrap((method, instance, exceptions, inner) =>
            {
                Console.WriteLine("Inner Before");
                inner.Execute(method, instance, exceptions);
                Console.WriteLine("Inner After");
            })
            .Wrap((method, instance, exceptions, inner) =>
            {
                Console.WriteLine("Outer Before");
                inner.Execute(method, instance, exceptions);
                Console.WriteLine("Outer After");
            });

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Pass"), this, exceptions);

                exceptions.Any().ShouldBeFalse();
                console.Lines.ShouldEqual("Outer Before", "Inner Before", "Pass", "Inner After", "Outer After");
            }
        }

        public void ShouldHandleCatastrophicExceptionsWhenBehaviorsThrowRatherThanContributeExceptions()
        {
            builder.Wrap((method, instance, exceptions, inner) =>
            {
                throw new Exception("Unsafe behavior threw!");
            });

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Pass"), this, exceptions);

                exceptions.Count.ShouldEqual(1);
                console.Lines.ShouldEqual();
            }
        }

        public void ShouldAllowWrappingTheBehaviorInSetUpTearDown()
        {
            builder.SetUpTearDown(SetUp, TearDown);

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Pass"), this, exceptions);

                exceptions.Any().ShouldBeFalse();
                console.Lines.ShouldEqual("SetUp", "Pass", "TearDown");
            }

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Fail"), this, exceptions);

                exceptions.Count.ShouldEqual(1);
                console.Lines.ShouldEqual("SetUp", "Fail Threw!", "TearDown");
            }
        }

        public void ShouldShortCircuitInnerBehaviorAndTearDownWhenSetupContributesExceptions()
        {
            builder.SetUpTearDown(FailingSetUp, TearDown);

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Pass"), this, exceptions);

                exceptions.Count.ShouldEqual(1);
                console.Lines.ShouldEqual("FailingSetUp Contributes an Exception!");
            }
        }

        public void ShouldNotShortCircuitTearDownWhenInnerBehaviorContributesExceptions()
        {
            builder.SetUpTearDown(SetUp, FailingTearDown);

            using (var console = new RedirectedConsole())
            {
                var exceptions = new ExceptionList();

                builder.Behavior.Execute(Method("Fail"), this, exceptions);

                exceptions.Count.ShouldEqual(2);
                console.Lines.ShouldEqual("SetUp", "Fail Threw!", "FailingTearDown Contributes an Exception!");
            }
        }

        void Pass()
        {
            Console.WriteLine("Pass");
        }
        
        void Fail()
        {
            Console.WriteLine("Fail Threw!");
            throw new Exception();
        }

        void SetUp(MethodInfo method, object instance, ExceptionList exceptions, MethodBehavior inner)
        {
            Console.WriteLine("SetUp");
        }

        void FailingSetUp(MethodInfo method, object instance, ExceptionList exceptions, MethodBehavior inner)
        {
            Console.WriteLine("FailingSetUp Contributes an Exception!");
            exceptions.Add(new Exception());
        }

        void TearDown(MethodInfo method, object instance, ExceptionList exceptions, MethodBehavior inner)
        {
            Console.WriteLine("TearDown");
        }

        void FailingTearDown(MethodInfo method, object instance, ExceptionList exceptions, MethodBehavior inner)
        {
            Console.WriteLine("FailingTearDown Contributes an Exception!");
            exceptions.Add(new Exception());
        }

        static MethodInfo Method(string name)
        {
            return typeof(MethodBehaviorBuilderTests).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}