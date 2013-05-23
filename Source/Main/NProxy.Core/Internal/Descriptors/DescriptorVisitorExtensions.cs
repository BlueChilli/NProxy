﻿//
// NProxy is a library for the .NET framework to create lightweight dynamic proxies.
// Copyright © Martin Tamme
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NProxy.Core.Internal.Descriptors
{
    /// <summary>
    /// Represents descriptor visitor extensions.
    /// </summary>
    internal static class DescriptorVisitorExtensions
    {
        /// <summary>
        /// Visits all specified interface types.
        /// </summary>
        /// <param name="descriptorVisitor">The descriptor visitor.</param>
        /// <param name="interfaceTypes">The interface types.</param>
        public static void VisitInterfaces(this IDescriptorVisitor descriptorVisitor, IEnumerable<Type> interfaceTypes)
        {
            foreach (var interfaceType in interfaceTypes)
            {
                descriptorVisitor.VisitInterface(interfaceType);

                descriptorVisitor.VisitMembers(interfaceType);
            }
        }

        /// <summary>
        /// Visits all constructors of the specified type.
        /// </summary>
        /// <param name="descriptorVisitor">The descriptor visitor.</param>
        /// <param name="type">The type.</param>
        public static void VisitConstructors(this IDescriptorVisitor descriptorVisitor, Type type)
        {
            if (descriptorVisitor == null)
                throw new ArgumentNullException("descriptorVisitor");

            if (type == null)
                throw new ArgumentNullException("type");

            var constructorInfos = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var constructorInfo in constructorInfos)
            {
                descriptorVisitor.VisitConstructor(constructorInfo);
            }
        }

        /// <summary>
        /// Visits all members of the specified type.
        /// </summary>
        /// <param name="descriptorVisitor">The descriptor visitor.</param>
        /// <param name="type">The type.</param>
        public static void VisitMembers(this IDescriptorVisitor descriptorVisitor, Type type)
        {
            // Visit events.
            descriptorVisitor.VisitEvents(type);

            // Visit properties.
            descriptorVisitor.VisitProperties(type);

            // Visit methods.
            descriptorVisitor.VisitMethods(type);
        }

        /// <summary>
        /// Visits all events of the specified type.
        /// </summary>
        /// <param name="descriptorVisitor">The descriptor visitor.</param>
        /// <param name="type">The type.</param>
        private static void VisitEvents(this IDescriptorVisitor descriptorVisitor, Type type)
        {
            if (descriptorVisitor == null)
                throw new ArgumentNullException("descriptorVisitor");

            if (type == null)
                throw new ArgumentNullException("type");

            // Only visit instance events.
            var eventInfos = type.GetEvents(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var eventInfo in eventInfos)
            {
                descriptorVisitor.VisitEvent(eventInfo);
            }
        }

        /// <summary>
        /// Visits all properties of the specified type.
        /// </summary>
        /// <param name="descriptorVisitor">The descriptor visitor.</param>
        /// <param name="type">The type.</param>
        private static void VisitProperties(this IDescriptorVisitor descriptorVisitor, Type type)
        {
            if (descriptorVisitor == null)
                throw new ArgumentNullException("descriptorVisitor");

            if (type == null)
                throw new ArgumentNullException("type");

            // Only visit instance properties.
            var propertyInfos = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in propertyInfos)
            {
                descriptorVisitor.VisitProperty(propertyInfo);
            }
        }

        /// <summary>
        /// Visits all methods of the specified type.
        /// </summary>
        /// <param name="descriptorVisitor">The descriptor visitor.</param>
        /// <param name="type">The type.</param>
        private static void VisitMethods(this IDescriptorVisitor descriptorVisitor, Type type)
        {
            if (descriptorVisitor == null)
                throw new ArgumentNullException("descriptorVisitor");

            if (type == null)
                throw new ArgumentNullException("type");

            // Only visit non-accessor instance methods.
            var methodInfos = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                                  .Where(m => !m.IsSpecialName);

            foreach (var methodInfo in methodInfos)
            {
                descriptorVisitor.VisitMethod(methodInfo);
            }
        }
    }
}