using System;

namespace Sellorio.Validation;

/// <summary>
/// An exception used to short-circuit remaining validation logic and exit
/// validation with the messages accumilated up until the exception was thrown.
/// </summary>
public class FastFailException : Exception
{
}
