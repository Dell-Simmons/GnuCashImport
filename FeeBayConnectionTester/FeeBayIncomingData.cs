using CsvHelper.Configuration.Attributes;

namespace FeeBayConnectionTester
{
    public class FeeBayIncomingData
    {
        #region Constants and Fields
        private string _below_Standard_Performance_Fee;
        private string _charity_Donation;
        private string _deposit_Processing_Fee;
        private string _exchange_Rate;
        private string _feeBay_Collected_Tax;
        private string _fVF_Fixed;
        private string _fVF_Variable;
        private string _gross_Transaction_Amount;
        private string _international_Fee;
        private string _item_Not_As_Described_Fee;
        private string _item_Subtotal;
        private string _net_Amount;
        private string _quantity;
        private string _regulatory_Operating_Fee;
        private string _seller_Collected_Tax;
        private string _shipping_And_Handling;
        #endregion

        #region Public properties
        [Name("Below standard performance fee")]
        public required string Below_standard_performance_fee
        {
            get
            {
                if(_below_Standard_Performance_Fee == "--")
                {
                    _below_Standard_Performance_Fee = "0.00";
                }
                return _below_Standard_Performance_Fee;
            }
            set => _below_Standard_Performance_Fee = value;
        } //decimal

        [Name("Buyer name")]
        public required string Buyer_name { get; set; }

        [Name("Buyer username")]
        public required string Buyer_username { get; set; }

        [Name("Charity donation")]
        public required string Charity_donation
        {
            get
            {
                if(_charity_Donation == "--")
                {
                    _charity_Donation = "0.00";
                }
                return _charity_Donation;
            }
            set => _charity_Donation = value;
        } // decimal

        [Name("Deposit processing fee")]
        public required string Deposit_processing_fee
        {
            get
            {
                if(_deposit_Processing_Fee == "--")
                {
                    _deposit_Processing_Fee = "0.00";
                }
                return _deposit_Processing_Fee;
            }
            set => _deposit_Processing_Fee = value;
        } // decimal

        [Name("Description")]
        public required string Description { get; set; }

        [Name("Exchange rate")]
        public required string Exchange_rate
        {
            get
            {
                if(_exchange_Rate == "--")
                {
                    _exchange_Rate = "0.00";
                }
                return _exchange_Rate;
            }
            set => _exchange_Rate = value;
        } // decimal

        [Name("eBay collected tax")]
        public required string feeBay_collected_tax
        {
            get
            {
                if(_feeBay_Collected_Tax == "--")
                {
                    _feeBay_Collected_Tax = "0.00";
                }
                return _feeBay_Collected_Tax;
            }
            set => _feeBay_Collected_Tax = value;
        } // decimal

        [Name("Final Value Fee - fixed")]
        public required string FVF_fixed
        {
            get
            {
                if(_fVF_Fixed == "--")
                {
                    _fVF_Fixed = "0.00";
                }
                return _fVF_Fixed;
            }
            set => _fVF_Fixed = value;
        } // decimal

        [Name("Final Value Fee - variable")]
        public required string FVF_variable
        {
            get
            {
                if(_fVF_Variable == "--")
                {
                    _fVF_Variable = "0.00";
                }
                return _fVF_Variable;
            }
            set => _fVF_Variable = value;
        } // decimal

        [Name("Gross transaction amount")]
        public required string Gross_transaction_amount
        {
            get
            {
                if(_gross_Transaction_Amount == "--")
                {
                    _gross_Transaction_Amount = "0.00";
                }
                return _gross_Transaction_Amount;
            }
            set => _gross_Transaction_Amount = value;
        } // decimal

        [Name("International fee")]
        public required string International_fee
        {
            get
            {
                if(_international_Fee == "--")
                {
                    _international_Fee = "0.00";
                }
               
                    return _international_Fee;
            }
            set => _international_Fee = value;
        } // decimal

        [Name("Item ID")]
        public required string Item_ID { get; set; }

        [Name("Very high \"item not as described\" fee")]
        public required string Item_not_as_described_fee
        {
            get
            {
                if(_item_Not_As_Described_Fee == "--")
                {
                    _item_Not_As_Described_Fee = "0.00";
                }
                return _item_Not_As_Described_Fee;
            }
            set => _item_Not_As_Described_Fee = value;
        }// deicmal

        [Name("Item subtotal")]
        public required string Item_subtotal
        {
            get
            {
                if(_item_Subtotal == "--")
                {
                    _item_Subtotal = "0.00";
                }
                return _item_Subtotal;
            }
            set => _item_Subtotal = value;
        }// decimal

        [Name("Item title")]
        public required string Item_title { get; set; }

        [Name("Legacy order ID")]
        public required string Legacy_order_ID { get; set; }

        [Name("Net amount")]
        public required string Net_amount    //decimal
        {
            get
            {
                if(_net_Amount == "--")
                {
                    _net_Amount = "0.00";
                }
                return _net_Amount;
            }
            set => _net_Amount = value;
        }

        [Name("Order number")]
        public required string Order_number { get; set; }

        [Name("Payout currency")]
        public required string Payout_currency { get; set; }

        [Name("Payout date")]
        public required string Payout_date { get; set; } // datetime

        [Name("Payout ID")]
        public required string Payout_ID { get; set; }

        [Name("Payout method")]
        public required string Payout_method { get; set; }

        [Name("Payout status")]
        public required string Payout_status { get; set; }

        [Name("Quantity")]
        public required string Quantity // int
        {
            get
            {
                if(_quantity == "--")
                {
                    _quantity = "0";
                }
                return _quantity;
            }
            set => _quantity = value;
        }

        [Name("Reason for hold")]
        public required string Reason_for_hold { get; set; }

        [Name("Reference ID")]
        public required string Reference_ID { get; set; }

        [Name("Regulatory operating fee")]
        public required string Regulatory_operating_fee
        {
            get
            {
                if(_regulatory_Operating_Fee == "--")
                {
                    _regulatory_Operating_Fee = "0.00";
                }
                return _regulatory_Operating_Fee;
            }
            set => _regulatory_Operating_Fee = value;
        } // decimal

        [Name("Seller collected tax")]
        public required string Seller_collected_tax
        {
            get
            {
                if(_seller_Collected_Tax == "--")
                {
                    _seller_Collected_Tax = "0.00";
                }
                return _seller_Collected_Tax;
            }
            set => _seller_Collected_Tax = value;
        } // decimal

        [Name("Ship to city")]
        public required string Ship_to_city { get; set; }

        [Name("Ship to country")]
        public required string Ship_to_country { get; set; }

        [Name("Ship to province/region/state")]
        public required string Ship_to_state { get; set; }

        [Name("Ship to zip")]
        public required string Ship_to_zip { get; set; }

        [Name("Shipping and handling")]
        public required string Shipping_and_handling
        {
            get
            {
                if(_shipping_And_Handling == "--")
                {
                    _shipping_And_Handling = "0.00";
                }
                return _shipping_And_Handling;
            }
            set => _shipping_And_Handling = value;
        } // decimal

        [Name("Custom label")]
        public required string Sku { get; set; }

        [Name("Transaction creation date")]
        public required string Transaction_creation_date { get; set; } // datetime

        [Name("Transaction currency")]
        public required string Transaction_currency { get; set; }

        [Name("Transaction ID")]
        public required string Transaction_ID { get; set; }

        [Name("Type")]
        public required string Type { get; set; }
        #endregion
    }
}
