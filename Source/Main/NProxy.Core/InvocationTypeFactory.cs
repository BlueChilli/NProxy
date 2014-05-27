﻿//
// Copyright © Martin Tamme
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NProxy.Core.Internal.Reflection;
using NProxy.Core.Internal.Reflection.Emit;
using NProxy.Core.Intercept;

namespace NProxy.Core
{
    /// <summary>
    /// Represents an invocation type factory.
    /// </summary>
    internal sealed class InvocationTypeFactory : IInvocationTypeFactory
    {
        /// <summary>
        /// The <see cref="MethodBase.GetMethodFromHandle(RuntimeMethodHandle,RuntimeTypeHandle)"/> method information.
        /// </summary>
        private static readonly MethodInfo MethodBaseGetMethodFromHandleMethodInfo = typeof (MethodBase).GetMethod(
            "GetMethodFromHandle",
            BindingFlags.Public | BindingFlags.Static,
            typeof (RuntimeMethodHandle), typeof (RuntimeTypeHandle));

        /// <summary>
        /// The <see cref="EventInvocationBase"/> constructor information.
        /// </summary>
        private static readonly ConstructorInfo EventInvocationBaseConstructorInfo = typeof (EventInvocationBase).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            typeof (string), typeof (object), typeof (MethodInfo), typeof (bool), typeof (object[]));

        /// <summary>
        /// The <see cref="PropertyInvocationBase"/> constructor information.
        /// </summary>
        private static readonly ConstructorInfo PropertyInvocationBaseConstructorInfo = typeof (PropertyInvocationBase).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            typeof (string), typeof (object), typeof (MethodInfo), typeof (bool), typeof (object[]));

        /// <summary>
        /// The <see cref="MethodInvocationBase"/> constructor information.
        /// </summary>
        private static readonly ConstructorInfo MethodInvocationBaseConstructorInfo = typeof (MethodInvocationBase).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            typeof (object), typeof (MethodInfo), typeof (bool), typeof (object[]));

        /// <summary>
        /// The <see cref="MethodInvocationBase.InvokeBase(object,object[])"/> method information.
        /// </summary>
        private static readonly MethodInfo MethodInvocationBaseInvokeBaseMethodInfo = typeof (MethodInvocationBase).GetMethod(
            "InvokeBase",
            BindingFlags.NonPublic | BindingFlags.Instance,
            typeof (object), typeof (object[]));

        /// <summary>
        /// The <see cref="MethodInvocationBase.InvokeVirtual(object,object[])"/> method information.
        /// </summary>
        private static readonly MethodInfo MethodInvocationBaseInvokeVirtualMethodInfo = typeof (MethodInvocationBase).GetMethod(
            "InvokeVirtual",
            BindingFlags.NonPublic | BindingFlags.Instance,
            typeof (object), typeof (object[]));

        /// <summary>
        /// The type repository.
        /// </summary>
        private readonly IInvocationTypeRepository _typeRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationTypeFactory"/> class.
        /// </summary>
        /// <param name="typeRepository">The type repository.</param>
        public InvocationTypeFactory(IInvocationTypeRepository typeRepository)
        {
            if (typeRepository == null)
                throw new ArgumentNullException("typeRepository");

            _typeRepository = typeRepository;
        }

        /// <summary>
        /// Builds the type initializer.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="genericParameterTypes">The generic parameter types.</param>
        /// <param name="methodFieldInfo">The method information static field information.</param>
        private static void BuildTypeInitializer(TypeBuilder typeBuilder,
            MethodInfo methodInfo,
            Type[] genericParameterTypes,
            FieldInfo methodFieldInfo)
        {
            // Define type initializer.
            var typeInitializer = typeBuilder.DefineConstructor(
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName,
                CallingConventions.Standard,
                null);

            // Implement type initializer.
            var ilGenerator = typeInitializer.GetILGenerator();

            // Get and load target method information.
            var targetMethodInfo = methodInfo.MapGenericMethod(genericParameterTypes);
            var declaringType = targetMethodInfo.DeclaringType;

            ilGenerator.Emit(OpCodes.Ldtoken, targetMethodInfo);
            ilGenerator.Emit(OpCodes.Ldtoken, declaringType);
            ilGenerator.EmitCall(MethodBaseGetMethodFromHandleMethodInfo);

            // Store method information.
            ilGenerator.Emit(OpCodes.Castclass, typeof (MethodInfo));
            ilGenerator.Emit(OpCodes.Stsfld, methodFieldInfo);

            ilGenerator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Builds the constructor.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="name">The event name.</param>
        /// <param name="methodFieldInfo">The method information static field information.</param>
        /// <returns>The constructor information.</returns>
        private static void BuildEventConstructor(TypeBuilder typeBuilder, string name, FieldInfo methodFieldInfo)
        {
            // Define constructor.
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                EventInvocationBaseConstructorInfo.CallingConvention,
                new[] {typeof (object), typeof (bool), typeof (object[])},
                new[] {"target", "isOverride", "parameters"});

            // Implement constructor.
            var ilGenerator = constructorBuilder.GetILGenerator();

            // Load this reference.
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // Load name.
            ilGenerator.Emit(OpCodes.Ldstr, name);

            // Load target object.
            ilGenerator.Emit(OpCodes.Ldarg_1);

            // Load method information.
            ilGenerator.Emit(OpCodes.Ldsfld, methodFieldInfo);

            // Load override flag.
            ilGenerator.Emit(OpCodes.Ldarg_2);

            // Load parameters.
            ilGenerator.Emit(OpCodes.Ldarg_3);

            // Call parent constructor.
            ilGenerator.Emit(OpCodes.Call, EventInvocationBaseConstructorInfo);

            ilGenerator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Builds the constructor.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="name">The property name.</param>
        /// <param name="methodFieldInfo">The method information static field information.</param>
        /// <returns>The constructor information.</returns>
        private static void BuildPropertyConstructor(TypeBuilder typeBuilder, string name, FieldInfo methodFieldInfo)
        {
            // Define constructor.
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                PropertyInvocationBaseConstructorInfo.CallingConvention,
                new[] {typeof (object), typeof (bool), typeof (object[])},
                new[] {"target", "isOverride", "parameters"});

            // Implement constructor.
            var ilGenerator = constructorBuilder.GetILGenerator();

            // Load this reference.
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // Load name.
            ilGenerator.Emit(OpCodes.Ldstr, name);

            // Load target object.
            ilGenerator.Emit(OpCodes.Ldarg_1);

            // Load method information.
            ilGenerator.Emit(OpCodes.Ldsfld, methodFieldInfo);

            // Load override flag.
            ilGenerator.Emit(OpCodes.Ldarg_2);

            // Load parameters.
            ilGenerator.Emit(OpCodes.Ldarg_3);

            // Call parent constructor.
            ilGenerator.Emit(OpCodes.Call, PropertyInvocationBaseConstructorInfo);

            ilGenerator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Builds the constructor.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="methodFieldInfo">The method information static field information.</param>
        /// <returns>The constructor information.</returns>
        private static void BuildMethodConstructor(TypeBuilder typeBuilder, FieldInfo methodFieldInfo)
        {
            // Define constructor.
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                MethodInvocationBaseConstructorInfo.CallingConvention,
                new[] {typeof (object), typeof (bool), typeof (object[])},
                new[] {"target", "isOverride", "parameters"});

            // Implement constructor.
            var ilGenerator = constructorBuilder.GetILGenerator();

            // Load this reference.
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // Load target object.
            ilGenerator.Emit(OpCodes.Ldarg_1);

            // Load method information.
            ilGenerator.Emit(OpCodes.Ldsfld, methodFieldInfo);

            // Load override flag.
            ilGenerator.Emit(OpCodes.Ldarg_2);

            // Load parameters.
            ilGenerator.Emit(OpCodes.Ldarg_3);

            // Call parent constructor.
            ilGenerator.Emit(OpCodes.Call, MethodInvocationBaseConstructorInfo);

            ilGenerator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Builds the invoke method.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="genericParameterTypes">The generic parameter types.</param>
        /// <param name="isVirtual">A value indicating whether the method should be called virtually.</param>
        private static void BuildInvokeMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, Type[] genericParameterTypes, bool isVirtual)
        {
            var invokeMethodInfo = isVirtual ? MethodInvocationBaseInvokeVirtualMethodInfo : MethodInvocationBaseInvokeBaseMethodInfo;

            // Define method.
            var methodBuilder = typeBuilder.DefineMethod(invokeMethodInfo, false, true);

            methodBuilder.DefineParameters(invokeMethodInfo);

            // Implement method.
            var ilGenerator = methodBuilder.GetILGenerator();

            // Load target object.
            ilGenerator.Emit(OpCodes.Ldarg_1);

            // Load parameter values.
            var parameterTypes = methodInfo.MapGenericParameterTypes(genericParameterTypes);
            var parameterValueLocalBuilders = new LocalBuilder[parameterTypes.Length];

            LoadParameterValues(ilGenerator, 2, parameterTypes, parameterValueLocalBuilders);

            // Call target method.
            var targetMethodInfo = methodInfo.MapGenericMethod(genericParameterTypes);

            ilGenerator.Emit(isVirtual ? OpCodes.Callvirt : OpCodes.Call, targetMethodInfo);

            // Restore by reference parameter values.
            RestoreByReferenceParameterValues(ilGenerator, 2, parameterTypes, parameterValueLocalBuilders);

            // Handle return value.
            var returnType = methodInfo.MapGenericReturnType(genericParameterTypes);

            if (returnType.IsVoid())
                ilGenerator.Emit(OpCodes.Ldnull);
            else
                ilGenerator.EmitBox(returnType);

            ilGenerator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Loads the parameter values onto the stack.
        /// </summary>
        /// <param name="ilGenerator">The intermediate language generator.</param>
        /// <param name="argumentIndex">The argument index.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="parameterValueLocalBuilders">The parameter value local builders.</param>
        private static void LoadParameterValues(ILGenerator ilGenerator, int argumentIndex, IList<Type> parameterTypes, IList<LocalBuilder> parameterValueLocalBuilders)
        {
            for (var index = 0; index < parameterTypes.Count; index++)
            {
                var parameterType = parameterTypes[index];

                ilGenerator.EmitLoadArgument(argumentIndex);
                ilGenerator.EmitLoadValue(index);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);

                if (parameterType.IsByRef)
                {
                    var elementType = parameterType.GetElementType();
                    var parameterLocalBuilder = ilGenerator.DeclareLocal(elementType);

                    ilGenerator.EmitUnbox(elementType);
                    ilGenerator.Emit(OpCodes.Stloc, parameterLocalBuilder);
                    ilGenerator.Emit(OpCodes.Ldloca, parameterLocalBuilder);

                    parameterValueLocalBuilders[index] = parameterLocalBuilder;
                }
                else
                {
                    ilGenerator.EmitUnbox(parameterType);
                }
            }
        }

        /// <summary>
        /// Restores the by reference parameter values.
        /// </summary>
        /// <param name="ilGenerator">The intermediate language generator.</param>
        /// <param name="argumentIndex">The argument index.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="parameterValueLocalBuilders">The parameter value local builders.</param>
        private static void RestoreByReferenceParameterValues(ILGenerator ilGenerator, int argumentIndex, IList<Type> parameterTypes, IList<LocalBuilder> parameterValueLocalBuilders)
        {
            for (var index = 0; index < parameterTypes.Count; index++)
            {
                var parameterType = parameterTypes[index];

                if (!parameterType.IsByRef)
                    continue;

                var parameterLocalBuilder = parameterValueLocalBuilders[index];
                var elementType = parameterType.GetElementType();

                ilGenerator.EmitLoadArgument(argumentIndex);
                ilGenerator.EmitLoadValue(index);
                ilGenerator.Emit(OpCodes.Ldloc, parameterLocalBuilder);
                ilGenerator.EmitBox(elementType);
                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }
        }

        #region IInvocationTypeFactory Members

        /// <inheritdoc/>
        public Type CreateInvocationType(EventInfo eventInfo, MethodInfo methodInfo)
        {
            if (eventInfo == null)
                throw new ArgumentNullException("eventInfo");

            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            // Define type.
            var typeBuilder = _typeRepository.DefineType("EventInvocation", typeof (EventInvocationBase));

            // Define generic parameters.
            var genericParameterTypes = typeBuilder.DefineGenericParameters(methodInfo.GetGenericArguments());

            // Define method information static field.
            var methodFieldInfo = typeBuilder.DefineField(
                "TargetMethod",
                typeof (MethodInfo),
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

            // Build type initializer.
            BuildTypeInitializer(typeBuilder, methodInfo, genericParameterTypes, methodFieldInfo);

            // Build constructor.
            BuildEventConstructor(typeBuilder, eventInfo.Name, methodFieldInfo);

            // Build base invoke method only for non abstract and overrideable methods.
            if (!methodInfo.IsAbstract && methodInfo.CanOverride())
                BuildInvokeMethod(typeBuilder, methodInfo, genericParameterTypes, false);

            // Build virtual invoke method.
            BuildInvokeMethod(typeBuilder, methodInfo, genericParameterTypes, true);

            return typeBuilder.CreateType();
        }

        /// <inheritdoc/>
        public Type CreateInvocationType(PropertyInfo propertyInfo, MethodInfo methodInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            // Define type.
            var typeBuilder = _typeRepository.DefineType("PropertyInvocation", typeof (PropertyInvocationBase));

            // Define generic parameters.
            var genericParameterTypes = typeBuilder.DefineGenericParameters(methodInfo.GetGenericArguments());

            // Define method information static field.
            var methodFieldInfo = typeBuilder.DefineField(
                "TargetMethod",
                typeof (MethodInfo),
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

            // Build type initializer.
            BuildTypeInitializer(typeBuilder, methodInfo, genericParameterTypes, methodFieldInfo);

            // Build constructor.
            BuildPropertyConstructor(typeBuilder, propertyInfo.Name, methodFieldInfo);

            // Build base invoke method only for non abstract and overrideable methods.
            if (!methodInfo.IsAbstract && methodInfo.CanOverride())
                BuildInvokeMethod(typeBuilder, methodInfo, genericParameterTypes, false);

            // Build virtual invoke method.
            BuildInvokeMethod(typeBuilder, methodInfo, genericParameterTypes, true);

            return typeBuilder.CreateType();
        }

        /// <inheritdoc/>
        public Type CreateInvocationType(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            // Define type.
            var typeBuilder = _typeRepository.DefineType("MethodInvocation", typeof (MethodInvocationBase));

            // Define generic parameters.
            var genericParameterTypes = typeBuilder.DefineGenericParameters(methodInfo.GetGenericArguments());

            // Define method information static field.
            var methodFieldInfo = typeBuilder.DefineField(
                "TargetMethod",
                typeof (MethodInfo),
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

            // Build type initializer.
            BuildTypeInitializer(typeBuilder, methodInfo, genericParameterTypes, methodFieldInfo);

            // Build constructor.
            BuildMethodConstructor(typeBuilder, methodFieldInfo);

            // Build base invoke method only for non abstract and overrideable methods.
            if (!methodInfo.IsAbstract && methodInfo.CanOverride())
                BuildInvokeMethod(typeBuilder, methodInfo, genericParameterTypes, false);

            // Build virtual invoke method.
            BuildInvokeMethod(typeBuilder, methodInfo, genericParameterTypes, true);

            return typeBuilder.CreateType();
        }

        #endregion
    }
}