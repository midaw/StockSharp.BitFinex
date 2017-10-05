using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Serialization;
using StockSharp.BitStamp;
using StockSharp.Localization;
using StockSharp.Messages;

namespace StockSharp.BitFinex
{
    [DisplayName("BitFinex")]
 
   
    [CategoryLoc("Cryptocurrency")]

   public class BitFinexMessageAdapter : MessageAdapter
    {
        private SecureString _key;
        private SecureString _secret;
        private int? _clientId;
        private long _lookupSecuritiesId = 0;
        private long _lookupPortfoliosId = 0;

        

        public SecureString Key
        {
            get
            {
                return this._key;
            }
            set
            {
                this._key = value;
            }
        }
        public SecureString Secret
        {
            get
            {
                return this._secret;
            }
            set
            {
                this._secret = value;
            }
        }
        public int? ClientId
        {
            get
            {
                return this._clientId;
            }
            set
            {
                this._clientId = value;
            }
        }


        static BitFinexMessageAdapter()
        {

        }

        public BitFinexMessageAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
        {
            this.AddSupportedMessage(MessageTypes.MarketData);
            this.AddSupportedMessage(MessageTypes.SecurityLookup);

 

        }

        protected virtual bool IsSupportNativeSecurityLookup
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc />
        protected virtual bool IsSupportNativePortfolioLookup
        {
            get
            {
                return false;
            }
        }
        public override   OrderCondition CreateOrderCondition()
        {
            return (OrderCondition)new BitStampOrderCondition();
        }

        protected override void OnSendInMessage(Message message)
        {
            switch (message.Type)
            {
                case MessageTypes.Reset:
                {
                    _lookupSecuritiesId = 0;
                    _lookupPortfoliosId = 0;

                    if (_ibSocket != null)
                    {
                        try
                        {
                            DisposeSocket();
                        }
                        catch (Exception ex)
                        {
                            SendOutError(ex);
                        }

                        _ibSocket = null;
                    }

                    SendOutMessage(new ResetMessage());

                    break;
                }

                case MessageTypes.Connect:
                {
                    if (_ibSocket != null)
                        throw new InvalidOperationException("_ibSocket != null");

                    _ibSocket = new IBSocket();

                    _ibSocket.Connect(Address, Port);

                    break;
                }

                case MessageTypes.Disconnect:
                {
                    if (_ibSocket == null)
                        throw new InvalidOperationException(LocalizedStrings.Str1856);

                    _ibSocket.disconnect();

                    break;
                }

                case MessageTypes.OrderRegister:
                    // TODO
                    break;

                case MessageTypes.OrderCancel:
                    // TODO
                    break;

                case MessageTypes.OrderGroupCancel:
                    // TODO
                    break;

                case MessageTypes.OrderReplace:
                    // TODO
                    break;

                case MessageTypes.Portfolio:
                    // TODO
                    break;

                case MessageTypes.PortfolioLookup:
                    ProcessPortolioLookupMessage((PortfolioLookupMessage)message);
                    break;

                case MessageTypes.MarketData:
                    ProcessMarketDataMessage((MarketDataMessage)message);
                    break;

                case MessageTypes.SecurityLookup:
                    ProcessSecurityLookupMessage((SecurityLookupMessage)message);
                    break;
            }
        }

        private void IBSocket_NewSymbol(int row, int rowCount, long contractId, string name, string secCode, string secClass, int decimals, int lotSize, double stepPrice, double priceStep,
            string isin, string board, System.DateTime expiryDate, double daysBeforeExpiry, double strike)
        {
            if (secCode.IsEmpty())
                secCode = contractId.To<string>();

            var securityId = new SecurityId
            {
                SecurityCode = secCode,
                BoardCode = board,
                NativeAsInt = contractId,
                Isin = isin
            };

            if (secClass.IsEmpty())
                secClass = board;

            DateTime? expDate = DateTime.FromOADate(0) == expiryDate ? (DateTime?)null : expiryDate;

            var secMsg = new SecurityMessage
            {
                PriceStep =Convert.ToDecimal( priceStep.ToDecimal()),
                Decimals = decimals,
                Multiplier = lotSize,
                Name = name,
                ShortName = name,
                ExpiryDate = expDate == null ? (DateTimeOffset?)null : expDate.Value.ApplyTimeZone(TimeHelper.Est),
                ExtensionInfo = new Dictionary<string, object>
                {
                    { "Class", secClass }
                },
                OriginalTransactionId = _lookupSecuritiesId
            };

            SendOutMessage(secMsg);
        }
        private void IBSocket_Connected()
        {
            SendOutMessage(new ConnectMessage());
        }
        private void ProcessPortolioLookupMessage(PortfolioLookupMessage pfMsg)
        {
            if (_lookupPortfoliosId == 0)
            {
                _lookupPortfoliosId = pfMsg.TransactionId;
                _ibSocket.GetPrortfolioList();
            }
            else
                SendOutError(LocalizedStrings.Str1868);
        }

        private void ProcessSecurityLookupMessage(SecurityLookupMessage message)
        {
            if (_lookupSecuritiesId == 0)
            {
                _lookupSecuritiesId = message.TransactionId;
                _ibSocket.GetSymbols();
            }
            else
                SendOutError(LocalizedStrings.Str1854);
        }

        private void IBSocket_NewPortfolio(int row, int nrows, string portfolioName, string portfolioExch)
        {
            SendOutMessage(new PortfolioMessage
            {
                PortfolioName = portfolioName,
                BoardCode = "BoardCode",
                ExtensionInfo = new Dictionary<string, object>
                {
                    { "PortfolioStatus", "status" }
                }
            });

            if ((row + 1) < nrows)
                return;

            SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = _lookupPortfoliosId });
            _lookupPortfoliosId = 0;
        }

        private void ProcessMarketDataMessage(MarketDataMessage mdMsg)
        {
            var contractId = mdMsg.SecurityId.NativeAsInt;

            switch (mdMsg.DataType)
            {
                case MarketDataTypes.Level1:
                {
                    //TODO
                    break;
                }
                case MarketDataTypes.MarketDepth:
                {
                    //TODO
                    break;
                }
                case MarketDataTypes.Trades:
                {
                    if (mdMsg.From == null)
                    {
                        if (mdMsg.IsSubscribe)
                            _ibSocket.ListenTicks(contractId);
                        else
                            _ibSocket.CancelTicks(contractId);
                    }
                    else
                    {
                        //TODO
                    }

                    break;
                }
                case MarketDataTypes.CandleTimeFrame:
                {
                    //TODO
                    break;
                }
                default:
                {
                    SendOutMarketDataNotSupported(mdMsg.TransactionId);
                    return;
                }
            }
        }
        private void IBSocket_NewTick(int contractId, System.DateTime time, double price, double volume, string tradeId)
        {
            SendOutMessage(CreateTrade(contractId, time, price.ToDecimal(), volume.ToDecimal(), tradeId.To<long>()));
        }

        private static ExecutionMessage CreateTrade(long contractId, DateTime time, decimal? price, decimal? volume, long tradeId)
        {
            return new ExecutionMessage
            {
                SecurityId = new SecurityId { NativeAsInt = contractId },
                TradeId = tradeId,
                TradePrice = price,
                TradeVolume = volume,
                ServerTime = time.ApplyTimeZone(TimeHelper.Est),
                ExecutionType = ExecutionTypes.Tick
            };
        }


        public override void Save(SettingsStorage storage)
        {
            base.Save(storage);
            storage.SetValue<SecureString>("Key", this.Key);
            storage.SetValue<SecureString>("Secret", this.Secret);
            storage.SetValue<int?>("ClientId", this.ClientId);
        }
        public override void Load(SettingsStorage storage)
        {
            base.Load(storage);
            this.Key = storage.GetValue<SecureString>("Key", (SecureString)null);
            this.Secret = storage.GetValue<SecureString>("Secret", (SecureString)null);
            this.ClientId = storage.GetValue<int?>("ClientId", new int?());
        }
    }
    public static class MyExtensions
    {
        public static decimal ToDecimal(this double original)
        {

            return Convert.ToDecimal(original);
           
        }
    }

}
