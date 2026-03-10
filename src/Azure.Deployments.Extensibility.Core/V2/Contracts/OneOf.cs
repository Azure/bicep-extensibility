// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts
{
    /// <summary>
    /// A discriminated union that holds exactly one of two possible values.
    /// </summary>
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

        [MemberNotNullWhen(true, nameof(AsT0))]
        public bool IsT0 => this.index == 0;

        [MemberNotNullWhen(true, nameof(AsT1))]
        public bool IsT1 => this.index == 1;

        public T0? AsT0 => this.value0;

        public T1? AsT1 => this.value1;

        public TResult Match<TResult>(Func<T0, TResult> onT0, Func<T1, TResult> onT1) => this.index switch
        {
            0 => onT0(this.value0!),
            1 => onT1(this.value1!),
            _ => throw new UnreachableException(),
        };

        public void Switch(Action<T0> onT0, Action<T1> onT1)
        {
            switch (this.index)
            {
                case 0: onT0(this.value0!); break;
                case 1: onT1(this.value1!); break;
                default: throw new UnreachableException();
            }
        }

        public static implicit operator OneOf<T0, T1>(T0 value) => new(0, value, default);
        public static implicit operator OneOf<T0, T1>(T1 value) => new(1, default, value);
    }

    /// <summary>
    /// A discriminated union that holds exactly one of three possible values.
    /// </summary>
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

        [MemberNotNullWhen(true, nameof(AsT0))]
        public bool IsT0 => this.index == 0;

        [MemberNotNullWhen(true, nameof(AsT1))]
        public bool IsT1 => this.index == 1;

        [MemberNotNullWhen(true, nameof(AsT2))]
        public bool IsT2 => this.index == 2;

        public T0? AsT0 => this.value0;

        public T1? AsT1 => this.value1;

        public T2? AsT2 => this.value2;

        public TResult Match<TResult>(Func<T0, TResult> onT0, Func<T1, TResult> onT1, Func<T2, TResult> onT2) => this.index switch
        {
            0 => onT0(this.value0!),
            1 => onT1(this.value1!),
            2 => onT2(this.value2!),
            _ => throw new UnreachableException(),
        };

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

        public static implicit operator OneOf<T0, T1, T2>(T0 value) => new(0, value, default, default);
        public static implicit operator OneOf<T0, T1, T2>(T1 value) => new(1, default, value, default);
        public static implicit operator OneOf<T0, T1, T2>(T2 value) => new(2, default, default, value);
    }
}
