/*<FILE_LICENSE>
* NFX (.NET Framework Extension) Unistack Library
* Copyright 2003-2014 IT Adapter Inc / 2015 Aum Code LLC
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NFX;
using NFX.Environment;
using NFX.Financial;
using NFX.ServiceModel;
using NFX.Log;

namespace NFX.Web.Pay
{
  /// <summary>
  /// Denotes transaction types, such as: charges, refunds, transfers
  /// </summary>
  public enum TransactionType 
  { 
    /// <summary>
    /// Customer paid for some service (i.e. bought some product).
    /// Funds got withdrawn from customer account (i.e. credit card) into another account (i.e. system account)
    /// </summary>
    Charge, 

    /// <summary>
    /// The portion or the whole amount of the original charge has been refunded from system account 
    /// into customer account (i.e. credit card).
    /// RelatedTransactionID points to the original charge transaction
    /// </summary>
    Refund, 

    /// <summary>
    /// Funds were transfersed between accounts.
    /// This mode is usually used to transfer funds between system-internal accounts (i.e. from sales GL account to bank account)
    /// </summary>
    Transfer
  };


  /// <summary>
  /// Represents an abstraction of financial operation performed with pay system
  /// </summary>
  [Serializable]
  public sealed class Transaction
  {
    #region ctor

      /// <summary>
      /// Framework-only method. Developer shouldn't call it.
      /// </summary>
    internal Transaction(object id, TransactionType type, 
                          Account from, Account to, 
                          string processorName, object processorToken, 
                          Amount amount, DateTime createDateUTC, string description, 
                          object relativeTransactionID = null,
                          //List<object> childTransactionIDs = null, 
                          //object parentTransactionID = null, object childTransactionID = null, 
                          object extraData = null, 
                          bool isCaptured = false, bool canRefund = true, bool isRefunded = false)
      {
        m_ID = id;
        m_TransactionType = type;

        m_From = from;
        m_To = to;

        m_ProcessorName = processorName;
        m_ProcessorToken = processorToken;

        m_Amount = amount;
        m_CreateDateUTC = createDateUTC;
        m_Description = description;

        m_RelativeTransactionID = relativeTransactionID;
        //m_ChildTransactionIDs = childTransactionIDs;

        m_ExtraData = extraData;

        m_IsCaptured = isCaptured;
        m_IsRefunded = isRefunded;
        m_CanRefund = canRefund;
      }

    #endregion

    #region Fields

      private object m_ID;
      private TransactionType m_TransactionType;

      private Account m_From;
      private Account m_To;

      private string m_ProcessorName;
      private object m_ProcessorToken;
      private DateTime m_CreateDateUTC;
      private string m_Description;

      private Amount m_Amount;
      private Amount m_AmountCaptured;
      private Amount m_AmountRefunded;

      private object m_RelativeTransactionID;

      private object m_ExtraData;

      private bool m_CanCapture;
      private bool m_IsCaptured;

      private bool m_Refundable;
      private bool m_CanRefund;
      private bool m_IsRefunded;

    #endregion

    #region Properties

      /// <summary>
      /// Unique ID for this transaction instance
      /// </summary>
      public object ID { get { return m_ID; } }

      public TransactionType Type { get { return m_TransactionType; } }

      public Account From { get { return m_From; } }
      public Account To { get { return m_To; } }

      /// <summary>
      /// The name of pay system implementation system that processes this transaction
      /// </summary>
      public string ProcessorName { get { return m_ProcessorName ?? string.Empty; } }

      private IPaySystem PaySystem { get { return NFX.Web.Pay.PaySystem.Instances[ProcessorName]; } }

      /// <summary>
      /// A value assigned by a particular pay system implementation.
      /// This value is parsed and understood by issuing system
      /// </summary>
      public object ProcessorToken { get { return m_ProcessorToken; } }

      /// <summary>
      /// UTC timestamp of this transaction creation
      /// </summary>
      public DateTime CreateDateUTC { get { return m_CreateDateUTC; } }

      /// <summary>
      /// Textual transaction description
      /// </summary>
      public string Description { get { return m_Description ?? string.Empty; } }

      /// <summary>
      /// Transaction amount - monetary value and currency code.
      /// Depending on the transaction type can indicate charged amount, refunded amount, transfer amount
      /// </summary>
      public Amount Amount { get { return m_Amount; } }

      /// <summary>
      /// Charged amount - monetary value and currency code.
      /// For charge indicates captured amount in two-step charge scenario.
      /// Returns zero for uncaptured charges (created with captured flag set to false).
      /// Returns non-zero amount (typically is equal to original charge amount) for captured amount.
      /// Returns zero for all other transaction types
      /// </summary>
      public Amount AmountCaptured { get { return m_AmountCaptured; } }

      /// <summary>
      /// Transaction amount - monetary value and currency code.
      /// Indicates refunded amount for charge transaction.
      /// Can be reflected in child refund transactions when explicit refund performed
      /// or can be just reminder of partial capture.
      /// For transactions types other than charge returns zero
      /// </summary>
      public Amount AmountRefunded { get { return m_AmountRefunded; } }

      /// <summary>
      /// ID of transaction that this trasaction relates/belongs to
      /// e.g. charge transaction ID for this refund transaction
      /// </summary>
      public object ParentTransactionID { get { return m_RelativeTransactionID; } }

      ///// <summary>
      ///// Contains IDs of transactions depending on this transaction
      ///// e.g. charge transaction can have one or many related refund transactions
      ///// </summary>
      //public List<object> ChildTransactionIDs 
      //{ 
      //  get 
      //  {
      //    if (m_ChildTransactionIDs == null) m_ChildTransactionIDs = new List<object>();
      //    return m_ChildTransactionIDs; 
      //  } 
      //}

      /// <summary>
      /// Contains additional data
      /// </summary>
      public object ExtraData { get { return m_ExtraData; } }

      /// <summary>
      /// Indicates if this transaction (charge) can be captured
      /// </summary>
      public bool CanCapture { get { return m_TransactionType == TransactionType.Charge && !m_IsCaptured; } }

      /// <summary>
      /// Indicates if this transaction (charge) has already been captured
      /// </summary>
      public bool IsCaptured { get { return m_IsCaptured;} }


      /// <summary>
      /// Indicates if this transaction can be refunded due to not amount reasons
      /// e.g. charge refund period is expired
      /// </summary>
      public bool Refundable { get { return m_Refundable; } }

      /// <summary>
      /// Trasaction can be refund if all conditions below are met:
      ///   - transaction type is Charge
      ///   - transaction flag IsRefunded is false 
      ///   - summary amount of all child refund transactions (if present) is less than this charge amount.
      /// Note that this method may cause loading of all child refund transactions so can be very slow  
      /// </summary>
      public bool CanRefund 
      { 
        get 
        {
          if (m_TransactionType != TransactionType.Charge) return false;
          if (LeftToRefund.Value <= 0) return false;

          return true;
        } 
      }

      /// <summary>
      /// Indicates that this transaction has been COMPLETELY refunded.
      /// Note that even if PART of original amount has been refunded this property returns false
      /// </summary>
      public bool IsRefunded { get { return LeftToRefund.Value > 0; } }

      /// <summary>
      /// Returns amount that can be refunded.
      /// It is calculated as 'this transaction amount' - 'summary amount of all child refund transactions (if present)'
      /// </summary>
      public Amount LeftToRefund { get { return m_Amount - m_AmountRefunded; } }

    #endregion

    #region Public methods

      /// <summary>
      /// Performs Capture operation on this transaction if possible.
      /// If an error occured PaymentException (or inherited) is thrown.
      /// Developers! Don't call PaySystem.Capture directly - always use this method instead
      /// </summary>
      public Transaction Capture(ITransactionContext context, Amount? amount = null, string description = null, object extraData = null) 
      {
        if (m_IsCaptured)
          throw new PaymentException(StringConsts.PAYMENT_CANNOT_CAPTURE_CAPTURED_PAYMENT_ERROR + this.GetType() + 
            ".Capture(transaction='{0}')".Args(this));

        var self = this;

        using(var pss = this.PaySystem.StartSession())
          pss.Capture(context, ref self, amount, description, extraData);

        self.m_IsCaptured = true;
        return self;
      }

      /// <summary>
      /// Performs Refund operation on this transaction if possible.
      /// If an error occured PaymentException (or inherited) is thrown.
      /// Developers! Don't call PaySystem.Refund directly - always use this method instead
      /// </summary>
      public Transaction Refund(ITransactionContext context, Amount? amount = null, string description = null, object extraData = null) 
      {
        if (m_TransactionType != TransactionType.Charge || IsRefunded || !IsCaptured) 
          throw new PaymentException(StringConsts.PAYMENT_REFUND_CANNOT_BE_REFUNDED_ERROR.Args(this) + this.GetType().Name + ".Refund");

        //var alreadyRefundedAmountVal = getChildTransactions().Where(ta => ta.m_TransactionType == TransactionType.Refund).Sum(ta => ta.Amount.Value);
        if (m_AmountRefunded.Value >= m_Amount.Value)
          throw new PaymentException(StringConsts.PAYMENT_REFUND_CANNOT_BE_REFUNDED_ERROR.Args(this) + this.GetType().Name + ".Refund");

        if (amount.HasValue)
        {
          var amountToRefund = amount.Value;
          if (!m_Amount.IsSameCurrencyAs(amountToRefund))
            throw new PaymentException(StringConsts.PAYMENT_REFUND_CURRENCY_MUST_MATCH_CHARGE_ERROR + this.GetType().Name
              + ".Refund: charge.Currency='{0}', refund.Currency='{1}'".Args(m_Amount.CurrencyISO, amountToRefund.CurrencyISO));

          if (amountToRefund.Value + m_AmountRefunded.Value > m_Amount.Value)
            throw new PaymentException(StringConsts.PAYMENT_REFUND_EXCEEDS_CHARGE_ERROR.Args(amountToRefund.Value, m_AmountRefunded.Value, m_Amount.Value) + this.GetType().Name + ".Refund");
        }
        else
        {
          if (m_AmountRefunded.Value > 0)
            amount = m_Amount - m_AmountRefunded;
        }

        var self = this;

        Transaction refundTA = null;
        using(var pss = this.PaySystem.StartSession())
          refundTA = pss.Refund(context, ref self, amount, description, extraData);

        //ChildTransactionIDs.Add(refundTA.ID);
        refundTA.m_RelativeTransactionID = this.ID;

        if (amount.HasValue)
        {
          var justRefundedAmount = amount.Value;
          m_AmountRefunded += justRefundedAmount;
          m_IsRefunded = m_AmountRefunded >= m_Amount;//(amountToRefund.Value + m_AmountRefunded.Value >= m_Amount.Value);
        }
        else
        {
          //m_IsRefunded = (amountToRefund.Value + m_AmountRefunded.Value >= m_Amount.Value);
        }

        return refundTA;
      }
      
    #endregion

    #region Object overrides

      public override string ToString()
      {
        return "{0} {1} {2}, {3}, {4} -> {5}".Args(m_ProcessorName, m_TransactionType, m_ID, m_Amount, m_From, m_To);
      }

      public override int GetHashCode()
      {
        return m_ProcessorName.GetHashCode() ^ m_TransactionType.GetHashCode() ^ m_From.GetHashCode() ^ m_To.GetHashCode();
      }

      public override bool Equals(object obj)
      {
        return Equals(obj as Transaction);
      }

      public bool Equals(Transaction other)
      {
        if (other == null) return false;
        if (object.ReferenceEquals(this, other)) return true;

        return m_ID == other.m_ID && m_TransactionType == other.m_TransactionType
          && m_From == other.m_From && m_To == other.m_To
          && m_ProcessorName == other.ProcessorName && m_ProcessorToken == other.ProcessorToken && m_CreateDateUTC == other.m_CreateDateUTC
          && m_Description == other.m_Description && m_Amount == other.m_Amount 
          && m_RelativeTransactionID == other.m_RelativeTransactionID /*&& Enumerable.SequenceEqual(m_ChildTransactionIDs, other.m_ChildTransactionIDs)*/
          && m_ExtraData == other.m_ExtraData && m_CanRefund == other.m_CanRefund && m_IsRefunded == other.m_IsRefunded
          && m_CanCapture == other.m_CanCapture && m_IsCaptured == other.m_IsCaptured;
      }

      public static bool operator==(Transaction tran0, Transaction tran1)
      {
        if ((object)tran0 == null)
        {
          if ((object)tran1 == null)
            return true; // null == null
          else
            return false; // only left side == null
        }

        return tran0.Equals(tran1);
      }

      public static bool operator!=(Transaction tran0, Transaction tran1)
      {
        return !(tran0 == tran1);
      }

    #endregion

    #region pvt. impl.

      //private IEnumerable<Transaction> getChildTransactions()
      //{
      //  if (m_ChildTransactionIDs == null) yield break;

      //  foreach (var childID in m_ChildTransactionIDs)
      //  {
      //    var childTA = NFX.Web.Pay.PaySystem.PaySystemHost.FetchTransaction(null, childID);
      //    yield return childTA;
      //  }
      //}

    #endregion
  }
}
