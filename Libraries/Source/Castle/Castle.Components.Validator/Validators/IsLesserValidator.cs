// Copyright 2004-2008 Castle Project - http://www.castleproject.org/
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
namespace Castle.Components.Validator
{
	using System;
	using System.Collections;

	/// <summary>
	/// Specifies the data type the <see cref="IsGreaterValidator"/>
	/// is dealing with.
	/// </summary>
	public enum IsLesserValidationType
	{
		/// <summary>
		/// Value compare as Integer
		/// </summary>
		Integer,

		/// <summary>
		/// Value compare as Decimal
		/// </summary>
		Decimal,

		/// <summary>
		/// Value compare as Date
		/// </summary>
		Date,

		/// <summary>
		/// Value compare as DateTime
		/// </summary>
		DateTime
	}

	/// <summary>
	/// Comparing properties value and make sure it is lesser than one another.
	/// </summary>
	public class IsLesserValidator : AbstractValidator
	{
		#region Private variables

		private readonly string propertyToCompare;
		private readonly IsGreaterValidationType validationType;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a IsLesserValidator of the given type and target property.
		/// </summary>
		/// <param name="type">The type of data to compare.</param>
		/// <param name="propertyToCompare">The name of the property to compare.</param>
		public IsLesserValidator( IsGreaterValidationType type, string propertyToCompare )
		{
			this.validationType = type;
			this.propertyToCompare = propertyToCompare;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Target Property to compare
		/// </summary>
		public string PropertyToCompare
		{
			get { return propertyToCompare; }
		}

		/// <summary>
		/// Gets or sets the validation type for this validator. 
		/// </summary>
		public IsGreaterValidationType ValidationType
		{
			get { return validationType; }
		}

		#endregion

		#region Object Overrides

		/// <summary>
		/// Validate that the property value greater than the value requirements.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="fieldValue"><c>true</c> if the field value is greater than the target property</param>
		/// <returns></returns>
		public override bool IsValid(object instance, object fieldValue)
		{
			object refValue = GetFieldOrPropertyValue(instance, propertyToCompare);

			if( ( fieldValue == null || fieldValue.ToString() == "" ) && ( refValue == null || refValue.ToString() == "" ) ) return true;

			bool valid = false;

			switch (validationType)
			{
				case IsGreaterValidationType.Integer:
					int intRefVal;
					int intFieldValue;

					int.TryParse(refValue.ToString(), out intRefVal);
					int.TryParse(fieldValue.ToString(), out intFieldValue);

					valid = intFieldValue < intRefVal;

					break;

				case IsGreaterValidationType.Decimal:
					decimal decRefVal;
					decimal decFieldVal;

					decimal.TryParse(refValue.ToString(), out decRefVal);
					decimal.TryParse(fieldValue.ToString(), out decFieldVal);

					valid = decFieldVal < decRefVal;

					break;

				case IsGreaterValidationType.DateTime:
				case IsGreaterValidationType.Date:

					DateTime dateRefVal;
					DateTime dateFieldVal;

					DateTime.TryParse(refValue.ToString(), out dateRefVal);
					DateTime.TryParse(fieldValue.ToString(), out dateFieldVal);

					if (validationType == IsGreaterValidationType.DateTime)
					{
						valid = dateFieldVal < dateRefVal;
					}

					if (validationType == IsGreaterValidationType.Date)
					{
						valid = dateFieldVal < dateRefVal;
					}

					break;

				default:
					break;
			}

			return valid;
		}

		/// <summary>
		/// Applies the browser validation by setting up one or
		/// more input rules on <see cref="IBrowserValidationGenerator"/>.
		/// </summary>
		/// <param name="config">The config.</param>
		/// <param name="inputType">Type of the input.</param>
		/// <param name="generator">The generator.</param>
		/// <param name="attributes">The attributes.</param>
		/// <param name="target">The target.</param>
		public override void ApplyBrowserValidation( BrowserValidationConfiguration config, InputElementType inputType,
													IBrowserValidationGenerator generator, IDictionary attributes,
													string target )
		{
			base.ApplyBrowserValidation( config, inputType, generator, attributes, target );

			generator.SetAsLesserThan( target, propertyToCompare, validationType, BuildErrorMessage() );
		}

		/// <summary>
		/// Returns the key used to internationalize error messages
		/// </summary>
		/// <value></value>
		protected override string MessageKey
		{
			get { return MessageConstants.LesserThanMessage; }
		}

		/// <summary>
		/// Browser validation support
		/// </summary>
		public override bool SupportsBrowserValidation
		{
			get { return true; }
		}

		#endregion
	}
}