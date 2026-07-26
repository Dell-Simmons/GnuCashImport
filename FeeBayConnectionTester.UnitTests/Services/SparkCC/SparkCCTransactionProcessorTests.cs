using FeeBayConnectionTester.DTO;
using FeeBayConnectionTester.Services.SimpleFin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleFin.SimpleFinDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FeeBayConnectionTester.UnitTests.Services.SparkCC
{
    [TestClass]
    public class SparkCCTransactionProcessorTests
    {
        private SparkCCTransactionProcessor _processor = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _processor = new SparkCCTransactionProcessor();
        }

        [TestMethod]
        public async Task ProcessSparkCCTransactionsAsync_NullIncomingRecords_ThrowsArgumentNullException()
        {
            // Arrange
            List<SimpleFinTransaction>? incomingRecords = null;

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
                _processor.ProcessSparkCCTransactionsAsync(incomingRecords!));
        }

        [TestMethod]
        public async Task ProcessSparkCCTransactionsAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var incomingRecords = new List<SimpleFinTransaction>();

            // Act
            var result = await _processor.ProcessSparkCCTransactionsAsync(incomingRecords);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task ProcessSparkCCTransactionsAsync_SingleExpenseTransaction_ReturnsTwoSplits()
        {
            // Arrange
            var incomingRecords = new List<SimpleFinTransaction>
            {
                new()
                {
                    Amount = -50.25m,
                    Description = "some expense",
                    TransactionId = "txn_1",
                    PostedDateUnix = new DateTimeOffset(2023, 10, 27, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()
                }
            };

            // Act
            var result = await _processor.ProcessSparkCCTransactionsAsync(incomingRecords);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            var split1 = result.Single(s => s.SortOrder == 1);
            var split2 = result.Single(s => s.SortOrder == 2);

            Assert.AreEqual("Credit Card Liabilities:Spark Business Credit Card", split1.Account);
            Assert.AreEqual(50.25m, split1.Amount);
            Assert.AreEqual("Expenses:Miscellaneous", split2.Account);
            Assert.AreEqual(-50.25m, split2.Amount);
        }

        [TestMethod]
        public async Task ProcessSparkCCTransactionsAsync_SingleCashBackTransaction_ReturnsTwoSplits()
        {
            // Arrange
            var incomingRecords = new List<SimpleFinTransaction>
            {
                new()
                {
                    Amount = 25.00m,
                    Description = "CASH BACK",
                    Payee = "Cashback",
                    TransactionId = "txn_2",
                    PostedDateUnix = new DateTimeOffset(2023, 10, 28, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()
                }
            };

            // Act
            var result = await _processor.ProcessSparkCCTransactionsAsync(incomingRecords);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            
            var split1 = result.Single(s => s.SortOrder == 1);
            var split2 = result.Single(s => s.SortOrder == 2);

            Assert.AreEqual("Credit Card Liabilities:Spark Business Credit Card", split1.Account);
            Assert.AreEqual(25.00m, split1.Amount);
            Assert.AreEqual("Spark CC Cash Back", split2.Account);
            Assert.AreEqual(-25.00m, split2.Amount);
        }

        [TestMethod]
        public async Task ProcessSparkCCTransactionsAsync_SingleElectronicPayment_ReturnsTwoSplits()
        {
            // Arrange
            var incomingRecords = new List<SimpleFinTransaction>
            {
                new()
                {
                    Amount = 1000.00m,
                    Description = "ELECTRONIC PAYMENT",
                    Payee = "Electronic Payment",
                    TransactionId = "txn_3",
                    PostedDateUnix = new DateTimeOffset(2023, 10, 29, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()
                }
            };

            // Act
            var result = await _processor.ProcessSparkCCTransactionsAsync(incomingRecords);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            var split1 = result.Single(s => s.SortOrder == 1);
            var split2 = result.Single(s => s.SortOrder == 2);

            Assert.AreEqual("Credit Card Liabilities:Spark Business Credit Card", split1.Account);
            Assert.AreEqual(-1000.00m, split1.Amount);
            Assert.AreEqual("TCCU Business Checking", split2.Account);
            Assert.AreEqual(1000.00m, split2.Amount);
        }

        [TestMethod]
        public async Task ProcessSparkCCTransactionsAsync_MultipleMixedTransactions_ReturnsAggregatedList()
        {
            // Arrange
            var incomingRecords = new List<SimpleFinTransaction>
            {
                new() // Expense
                {
                    Amount = -50.25m,
                    Description = "some expense",
                    TransactionId = "txn_1",
                    PostedDateUnix = new DateTimeOffset(2023, 10, 27, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()
                },
                new() // Cash Back
                {
                    Amount = 25.00m,
                    Description = "CASH BACK",
                    Payee = "Cashback",
                    TransactionId = "txn_2",
                    PostedDateUnix = new DateTimeOffset(2023, 10, 28, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()
                },
                new() // Payment
                {
                    Amount = 1000.00m,
                    Description = "ELECTRONIC PAYMENT",
                    Payee = "Electronic Payment",
                    TransactionId = "txn_3",
                    PostedDateUnix = new DateTimeOffset(2023, 10, 29, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()
                }
            };

            // Act
            var result = await _processor.ProcessSparkCCTransactionsAsync(incomingRecords);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count); // 2 splits for each of the 3 transactions

            // Spot check one transaction
            var expenseSplits = result.Where(r => r.TransactionId == "txn_1").ToList();
            Assert.AreEqual(2, expenseSplits.Count);
            Assert.IsTrue(expenseSplits.Any(s => s.Account == "Credit Card Liabilities:Spark Business Credit Card" && s.Amount == 50.25m));
            Assert.IsTrue(expenseSplits.Any(s => s.Account == "Expenses:Miscellaneous" && s.Amount == -50.25m));
        }
    }
}
