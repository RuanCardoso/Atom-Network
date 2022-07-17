/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
namespace Atom.Core
{
    public delegate TResult AtomFunc<out TResult>();
    public delegate TResult AtomFunc<in T1, out TResult>(T1 arg1);
    public delegate TResult AtomFunc<in T1, in T2, out TResult>(T1 arg1, T2 arg2);
    public delegate TResult AtomFunc<in T1, in T2, in T3, out TResult>(T1 arg1, T2 arg2, T3 arg3);
    public delegate TResult AtomFunc<in T1, in T2, in T3, in T4, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate TResult AtomFunc<in T1, in T2, in T3, in T4, in T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate TResult AtomFunc<in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

    public delegate void AtomAction();
    public delegate void AtomAction<in T1>(T1 arg1);
    public delegate void AtomAction<in T1, in T2>(T1 arg1, T2 arg2);
    public delegate void AtomAction<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3);
    public delegate void AtomAction<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate void AtomAction<in T1, in T2, in T3, in T4, in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void AtomAction<in T1, in T2, in T3, in T4, in T5, in T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
}
#endif