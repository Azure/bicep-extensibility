// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts
{
    /// <summary>
    /// A discriminated union that holds exactly one of two possible values.
    /// </summary>
    /// <typeparam name="T0">The first possible type.</typeparam>
    /// <typeparam name="T1">The second possible type.</typeparam>
    public readonly struct OneOf<T0, T1>
    {
        private readonly int index;
        private readonly T0? value0;
        private readonly T1? value1;

        private OneOf(int index, T0? value0, T1? value1)
        {
            this.index = index;
            this.value0 = value0;
            this.value1 = value1;
        }

        /// <summary>Returns <see langword="true"/> if the union holds a <typeparamref name="T0"/> value.</summary>
        [MemberNotNullWhen(true, nameof(AsT0))]
        public bool IsT0 => this.index == 0;

        /// <summary>Returns <see langword="true"/> if the union holds a <typeparamref name="T1"/> value.</summary>
        [MemberNotNullWhen(true, nameof(AsT1))]
        public bool IsT1 => this.index == 1;

        /// <summary>Gets the <typeparamref name="T0"/> value, or <see langword="default"/> if the union holds a different type.</summary>
        public T0? AsT0 => this.value0;

        /// <summary>Gets the <typeparamref name="T1"/> value, or <see langword="default"/> if the union holds a different type.</summary>
        public T1? AsT1 => this.value1;

        /// <summary>
        /// Invokes one of two functions based on which type the union holds and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <param name="onT0">Function invoked when the union holds a <typeparamref name="T0"/> value.</param>
        /// <param name="onT1">Function invoked when the union holds a <typeparamref name="T1"/> value.</param>
        /// <returns>The result of the invoked function.</returns>
        public TResult Match<TResult>(Func<T0, TResult> onT0, Func<T1, TResult> onT1) => this.index switch
        {
            0 => onT0(this.value0!),
            1 => onT1(this.value1!),
            _ => throw new UnreachableException(),
        };

        /// <summary>
        /// Invokes one of two actions based on which type the union holds.
        /// </summary>
        /// <param name="onT0">Action invoked when the union holds a <typeparamref name="T0"/> value.</param>
        /// <param name="onT1">Action invoked when the union holds a <typeparamref name="T1"/> value.</param>
        public void Switch(Action<T0> onT0, Action<T1> onT1)
        {
            switch (this.index)
            {
                case 0: onT0(this.value0!); break;
                case 1: onT1(this.value1!); break;
                default: throw new UnreachableException();
            }
        }

        /// <summary>Implicitly converts a <typeparamref name="T0"/> value to a <see cref="OneOf{T0, T1}"/>.</summary>
        public static implicit operator OneOf<T0, T1>(T0 value) => new(0, value, default);
        /// <summary>Implicitly converts a <typeparamref name="T1"/> value to a <see cref="OneOf{T0, T1}"/>.</summary>
        public static implicit operator OneOf<T0, T1>(T1 value) => new(1, default, value);
    }

    /// <summary>
    /// A discriminated union that holds exactly one of three possible values.
    /// </summary>
    /// <typeparam name="T0">The first possible type.</typeparam>
    /// <typeparam name="T1">The second possible type.</typeparam>
    /// <typeparam name="T2">The third possible type.</typeparam>
    public readonly struct OneOf<T0, T1, T2>
    {
        private readonly int index;
        private readonly T0? value0;
        private readonly T1? value1;
        private readonly T2? value2;

        private OneOf(int index, T0? value0, T1? value1, T2? value2)
        {
            this.index = index;
            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
        }

        /// <summary>Returns <see langword="true"/> if the union holds a <typeparamref name="T0"/> value.</summary>
        [MemberNotNullWhen(true, nameof(AsT0))]
        public bool IsT0 => this.index == 0;

        /// <summary>Returns <see langword="true"/> if the union holds a <typeparamref name="T1"/> value.</summary>
        [MemberNotNullWhen(true, nameof(AsT1))]
        public bool IsT1 => this.index == 1;

        /// <summary>Returns <see langword="true"/> if the union holds a <typeparamref name="T2"/> value.</summary>
        [MemberNotNullWhen(true, nameof(AsT2))]
        public bool IsT2 => this.index == 2;

        /// <summary>Gets the <typeparamref name="T0"/> value, or <see langword="default"/> if the union holds a different type.</summary>
        public T0? AsT0 => this.value0;

        /// <summary>Gets the <typeparamref name="T1"/> value, or <see langword="default"/> if the union holds a different type.</summary>
        public T1? AsT1 => this.value1;

        /// <summary>Gets the <typeparamref name="T2"/> value, or <see langword="default"/> if the union holds a different type.</summary>
        public T2? AsT2 => this.value2;

        /// <summary>
        /// Invokes one of three functions based on which type the union holds and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <param name="onT0">Function invoked when the union holds a <typeparamref name="T0"/> value.</param>
        /// <param name="onT1">Function invoked when the union holds a <typeparamref name="T1"/> value.</param>
        /// <param name="onT2">Function invoked when the union holds a <typeparamref name="T2"/> value.</param>
        /// <returns>The result of the invoked function.</returns>
        public TResult Match<TResult>(Func<T0, TResult> onT0, Func<T1, TResult> onT1, Func<T2, TResult> onT2) => this.index switch
        {
            0 => onT0(this.value0!),
            1 => onT1(this.value1!),
            2 => onT2(this.value2!),
            _ => throw new UnreachableException(),
        };

        /// <summary>
        /// Invokes one of three actions based on which type the union holds.
        /// </summary>
        /// <param name="onT0">Action invoked when the union holds a <typeparamref name="T0"/> value.</param>
        /// <param name="onT1">Action invoked when the union holds a <typeparamref name="T1"/> value.</param>
        /// <param name="onT2">Action invoked when the union holds a <typeparamref name="T2"/> value.</param>
        public void Switch(Action<T0> onT0, Action<T1> onT1, Action<T2> onT2)
        {
            switch (this.index)
            {
                case 0: onT0(this.value0!); break;
                case 1: onT1(this.value1!); break;
                case 2: onT2(this.value2!); break;
                default: throw new UnreachableException();
            }
        }

        /// <summary>Implicitly converts a <typeparamref name="T0"/> value to a <see cref="OneOf{T0, T1, T2}"/>.</summary>
        public static implicit operator OneOf<T0, T1, T2>(T0 value) => new(0, value, default, default);
        /// <summary>Implicitly converts a <typeparamref name="T1"/> value to a <see cref="OneOf{T0, T1, T2}"/>.</summary>
        public static implicit operator OneOf<T0, T1, T2>(T1 value) => new(1, default, value, default);
        /// <summary>Implicitly converts a <typeparamref name="T2"/> value to a <see cref="OneOf{T0, T1, T2}"/>.</summary>
        public static implicit operator OneOf<T0, T1, T2>(T2 value) => new(2, default, default, value);
    }
}
