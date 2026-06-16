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
        public required string Below_standard_performance_fee
        {
            get;set;
          /*   get
            {
                if(_below_Standard_Performance_Fee == "--")
                {
                    _below_Standard_Performance_Fee = "0.00";
                }
                return _below_Standard_Performance_Fee;
            }
            set => _bel ow_Standard_Performance_Fee = value;*/
        } //decimal

        public required string Buyer_name { get; set; }

        public required string Buyer_username { get; set; }

        public required string Charity_donation
        {
            get;set;
           /*  get
            {
                if(_charity_Donation == "--")
                {
                    _charity_Donation = "0.00";
                }
                return _charity_Donation;
            }
            set => _charity_Donation = value; */
        } // decimal

        public required string Deposit_processing_fee
        {
            get;set;
          /*   get
            {
                if(_deposit_Processing_Fee == "--")
                {
                    _deposit_Processing_Fee = "0.00";
                }
                return _deposit_Processing_Fee;
            }
            set => _deposi t_Processing_Fee = value;*/
        } // decimal

        public required string Description { get; set; }

        public required string Exchange_rate
        {
            get;set;
          /*   get
            {
                if(_exchange_Rate == "--")
                {
                    _exchange_Rate = "0.00";
                }
                return _exchange_Rate;
            }
            set => _exchange_Rate = value; */
        } // decimal

        public required string feeBay_collected_tax
        {
            get;set;
           /*  get
            {
                if(_feeBay_Collected_Tax == "--")
                {
                    _feeBay_Collected_Tax = "0.00";
                }
                return _feeBay_Collected_Tax;
            }
            set => _feeBay_Collected_Tax = value; */
        } // decimal

        public required string FVF_fixed
        {
            get;set;
           /*  get
            {
                if(_fVF_Fixed == "--")
                {
                    _fVF_Fixed = "0.00";
                }
                return _fVF_Fixed;
            }
            set => _fVF_Fixed = value; */
        } // decimal

        public required string FVF_variable
        {
            get;set;
           /*  get
            {
                if(_fVF_Variable == "--")
                {
                    _fVF_Variable = "0.00";
                }
                return _fVF_Variable;
            }
            set => _fVF_Variable = value; */
        } // decimal

        public required string Gross_transaction_amount
        {
            get;set;
          /*   get
            {
                if(_gross_Transaction_Amount == "--")
                {
                    _gross_Transaction_Amount = "0.00";
                }
                return _gross_Transaction_Amount;
            }
            set => _gross_Transaction_Amount = value; */
        } // decimal

        public required string International_fee
        {
            get;set;
          /*   get
            {
                if(_international_Fee == "--")
                {
                    _international_Fee = "0.00";
                }
               
                    return _international_Fee;
            }
            set => _international_Fee = value; */
        } // decimal

        public required string Item_ID { get; set; }

        public required string Item_not_as_described_fee
        {
            get;set;
           /*  get
            {
                if(_item_Not_As_Described_Fee == "--")
                {
                    _item_Not_As_Described_Fee = "0.00";
                }
                return _item_Not_As_Described_Fee;
            }
            set => _item_Not_As_Described_Fee = value; */
        }// deicmal

        public required string Item_subtotal
        {
            get;set;
          /*   get
            {
                if(_item_Subtotal == "--")
                {
                    _item_Subtotal = "0.00";
                }
                return _item_Subtotal;
            }
            set => _item_Subtotal = value; */
        }// decimal

        public required string Item_title { get; set; }

        public required string Legacy_order_ID { get; set; }

        public required string Net_amount    //decimal
        {
            get;set;
          /*   get
            {
                if(_net_Amount == "--")
                {
                    _net_Amount = "0.00";
                }
                return _net_Amount;
            }
            set => _net_Amount = value; */
        }

        public required string Order_number { get; set; }

        public required string Payout_currency { get; set; }

        public required string Payout_date { get; set; } // datetime

        public required string Payout_ID { get; set; }

        public required string Payout_method { get; set; }

        public required string Payout_status { get; set; }

        public required string Quantity // int
        {
            get;set;
           /*  get
            {
                if(_quantity == "--")
                {
                    _quantity = "0";
                }
                return _quantity;
            }
            set => _quantity = value; */
        }

        public required string Reason_for_hold { get; set; }

        public required string Reference_ID { get; set; }

        public required string Regulatory_operating_fee
        {
            get;set;
          /*   get
            {
                if(_regulatory_Operating_Fee == "--")
                {
                    _regulatory_Operating_Fee = "0.00";
                }
                return _regulatory_Operating_Fee;
            }
            set => _regulatory_Operating_Fee = value; */
        } // decimal

        public required string Seller_collected_tax
        {
            get;set;
           /*  get
            {
                if(_seller_Collected_Tax == "--")
                {
                    _seller_Collected_Tax = "0.00";
                }
                return _seller_Collected_Tax;
            }
            set => _seller_Collected_Tax = value; */
        } // decimal

        public required string Ship_to_city { get; set; }

        public required string Ship_to_country { get; set; }

        public required string Ship_to_state { get; set; }

        public required string Ship_to_zip { get; set; }

        public required string Shipping_and_handling
        {
            get;set;
         /*    get
            {
                if(_shipping_And_Handling == "--")
                {
                    _shipping_And_Handling = "0.00";
                }
                return _shipping_And_Handling;
            }
            set => _shipping_And_Handling = value; */
        } // decimal

        public required string Sku { get; set; }

        public required string Transaction_creation_date { get; set; } // datetime

        public required string Transaction_currency { get; set; }

        public required string Transaction_ID { get; set; }

        public required string Type { get; set; }
        #endregion
    }
}
