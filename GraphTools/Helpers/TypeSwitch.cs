using System;

namespace GraphTools.Helpers
{
    /// <summary>
    /// Helps with type switching.
    /// </summary>
    static class TypeSwitch
    {
        /// <summary>
        /// Creates a switch for the given object.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Switch<TSource> On<TSource>(TSource source)
        {
            return new Switch<TSource>(source);
        }
    }

    /// <summary>
    /// Switch for object types.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    class Switch<TSource>
    {
        /// <summary>
        /// Type of the object.
        /// </summary>
        private TSource source;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source"></param>
        public Switch(TSource source)
        {
            this.source = source;
        }

        /// <summary>
        /// Handles a case of the type switch.
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public Switch<TSource> Case<TTarget>(Action<TTarget> action) where TTarget : TSource
        {
            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            if (targetType.IsAssignableFrom(sourceType))
            {
                action((TTarget)source);
            }

            return this;
        }
    }
}
