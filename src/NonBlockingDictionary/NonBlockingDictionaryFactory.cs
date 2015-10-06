﻿// Copyright (c) Vladimir Sadov. All rights reserved.
//
// This file is distributed under the MIT License. See LICENSE.md for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace NonBlocking
{
    public abstract class NonBlockingDictionary
    {
        internal NonBlockingDictionary() { }

        public static NonBlockingDictionary<TKey, TValue> Create<TKey, TValue>(
            int cLevel,
            int size,
            IEqualityComparer<TKey> comparer = null)
        {
            return Create<TKey, TValue>(comparer);
        }

        public static NonBlockingDictionary<TKey, TValue> Create<TKey, TValue>(
            IEqualityComparer<TKey> comparer = null)
        {
            NonBlockingDictionary<TKey, TValue> result;

            if (default(TKey) == null)
            {
                if (typeof(TKey) == typeof(ValueType) ||
                    !(default(TKey) is ValueType))
                {
                    return CreateRefUnsafe<TKey, TValue>(comparer);
                }
            }
            else
            {
                if (typeof(TKey) == typeof(int))
                {
                    if (comparer == null)
                    {
                        return (NonBlockingDictionary<TKey, TValue>)(object)new NonBlockingDictionaryIntNoComparer<TValue>();
                    }

                    result = (NonBlockingDictionary<TKey, TValue>)(object)new NonBlockingDictionaryInt<TValue>();
                    result.keyComparer = comparer;
                    return result;
                }

                if (typeof(TKey) == typeof(long))
                {
                    if (comparer == null)
                    {
                        return (NonBlockingDictionary<TKey, TValue>)(object)new NonBlockingDictionaryLongNoComparer<TValue>();
                    }

                    result = (NonBlockingDictionary<TKey, TValue>)(object)new NonBlockingDictionaryLong<TValue>();
                    result.keyComparer = comparer;
                    return result;
                }
            }

            result = new NonBlockingDictionaryBoxed<TKey, TValue>();
            result.keyComparer = comparer ?? EqualityComparer<TKey>.Default;
            return result;
        }

        internal static NonBlockingDictionary<TKey, TValue> CreateRefUnsafe<TKey, TValue>(IEqualityComparer<TKey> comparer = null)
        {
            return (NonBlockingDictionary<TKey, TValue>)typeof(NonBlockingDictionary).
                GetMethod("CreateRef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).
                MakeGenericMethod(new Type[] { typeof(TKey), typeof(TValue) }).
                Invoke(null, new object[] { comparer });

            //var result = (NonBlockingDictionary<TKey, TValue>)
            //    Activator.CreateInstance(typeof(NonBlockingDictionaryRef<,>).MakeGenericType(new Type[] { typeof(TKey), typeof(TValue) }));

            //result.keyComparer = comparer ?? EqualityComparer<TKey>.Default;
            //return result;
        }

        internal static NonBlockingDictionary<TKey, TValue> CreateRef<TKey, TValue>(IEqualityComparer<TKey> comparer = null)
            where TKey : class
        {
            var result = new NonBlockingDictionaryRef<TKey, TKey, TValue>();
            result.keyComparer = comparer ?? EqualityComparer<TKey>.Default;
            return result;
        }

        ///////////////
        // Internal statics that do not need to be generic on TKey/TValue

        internal sealed class Prime
        {
            internal object originalValue;

            public Prime(object originalValue)
            {
                this.originalValue = originalValue;
            }
        }

        internal static readonly object TOMBSTONE = new object();
        internal static readonly Prime TOMBPRIME = new Prime(TOMBSTONE);
        internal static readonly object NULLVALUE = new object();
    }
}