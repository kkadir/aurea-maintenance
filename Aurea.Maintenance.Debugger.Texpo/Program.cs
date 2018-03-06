﻿
using Aurea.Maintenance.Debugger.Common.Extensions;

namespace Aurea.Maintenance.Debugger.Texpo
{
    using System;
    using System.Collections;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Linq;
    using System.Transactions;
    using Common;
    using Common.Models;
    using Aurea.Logging;
    using CIS.BusinessEntity;
    using CIS.Clients.Texpo.Import;
    using CIS.Enum.Enrollment;
    using CIS.Web.Services.Clients.Texpo;
    using CIS.Clients.Texpo;
    using CIS.BusinessComponent;
    using System.Collections.Generic;
    using CIS.Framework.Data;
    using Util = CIS.Framework.Data.Utility;
    using System.Xml;

    public class Program
    {
        public class MyMaintenance : CIS.Clients.Texpo.Maintenance
        {
            public MyMaintenance(string connectionCsr, string connectionMarket, string connectionAdmin)
                : base(connectionCsr, connectionMarket, connectionAdmin)
            {
                //
            }

            public override void InitializeVariables(string maintenanceFunction)
            {
                _runHour = "*";
                _runDay = "*";
                _runDayOfWeek = "*";
                _isEnabled = true;
                SkipIsValidRuntimeVerification = true;
                _lastRunTime = DateTime.Now.AddYears(-1);
            }
        }

        private static ClientEnvironmentConfiguration _clientConfig;
        private static GlobalApplicationConfigurationDS.GlobalApplicationConfiguration _appConfig;
        private static ILogger _logger = new Logger();
        private static readonly string _appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private static readonly string _mockDataDir = Path.Combine(_appDir, "MockData");

        public class MyExport : CIS.Clients.Texpo.Export.MainProcess//CIS.Export.BaseExport
        {
            private static readonly string _uaaDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "uua\\");
            private static readonly string _uqcDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) , "uqc\\");

            public MyExport(string connectionMarket, string connectionCsr, string connectionAdmin)
            {
                _connectionMarket = connectionMarket;
                _connectionCsr = connectionCsr;
                _connectionAdmin = connectionAdmin;
                _serviceExecuteDate = DateTime.Now;
                LoadConfiguration(string.Empty);
            }

            public void MyExportTransactions()
            {
                ExportTransactions();
            }

            public void MyTransmitFiles()
            {
                TransmitFiles();
            }

            public void MyExportLoadForecasting()
            {
                //ExportLoadForecasting();//private and exists on Texpo only, so if we need to debug this we need to implement logic here again
            }

            public void MyProcessEFTPayments()
            {
                ProcessEFTPayments();//protected virtual
            }

            public void MyRunAutoDNPProcess()
            {
                //RunAutoDNPProcess();//private and exists on Texpo only, so if we need to debug this we need to implement logic here again
            }

            public void MyLogService()
            {
                LogService();//protected derived from BaseService
            }

            public override void InitializeVariables(string exportFunction)
            {
                _directoryFtpOut = Path.Combine(_uaaDir, @"Data\ClientData\TXP\Services\Transport\Ftp\Out\");
                _directoryEncrypted = Path.Combine(_uaaDir, @"Data\Clientdata\TXP\Services\Transport\Ftp\Out\Market\Encrypted\");
                _directoryDecrypted = Path.Combine(_uaaDir, @"Data\Clientdata\TXP\Services\Transport\Ftp\Out\Market\Decrypted\");
                _directoryArchive = Path.Combine(_uaaDir, @"Data\Clientdata\TXP\Services\TransportArchive\Ftp\Out\");
                _directoryException = Path.Combine(_uaaDir, @"Data\Clientdata\TXP\Services\Transport\Ftp\Out\Market\Exception\");
                _pgpPassPhrase = "hokonxoyg";
                _pgpEncryptionKey = Path.Combine(_uqcDir, @"Data\PGPKeys\ista-na.asc");
                _pgpSignatureKey = Path.Combine(_uaaDir, @"Data\PGPKeys\ista-na.asc");
                _ftpRemoteServer = "localhost";
                _ftpRemoteDirectory = string.Empty;
                _ftpUserName = string.Empty;
                _ftpUserPassword = string.Empty;
                _runHour = "6";
                _runDay = "*";
                _runDayOfWeek = "*";
                _marketFileVersion = "3.0";
                _serviceInterval = 5;
                _historicalUsageRequestType = "HU";
                _clientID = _clientConfig.ClientId;
                if(!Directory.Exists(_directoryDecrypted))
                    Directory.CreateDirectory(_directoryDecrypted);

                if (!Directory.Exists(_directoryEncrypted))
                    Directory.CreateDirectory(_directoryEncrypted);

                if (!Directory.Exists(_directoryException))
                    Directory.CreateDirectory(_directoryException);


                if (!File.Exists(_pgpEncryptionKey))
                {
                    Directory.CreateDirectory(_pgpEncryptionKey.Replace(Path.GetFileName(_pgpEncryptionKey),""));
                    File.WriteAllText(_pgpEncryptionKey, @"-----BEGIN PGP PRIVATE KEY BLOCK-----
Version: BCPG C# v1.6.1.0

lQOsBFn53zQBCADMlxdkpJRay9J1lA2Xc6fsyM0A7CRzcY5TCuU31ZROV0s8Z+97
hpwAdRzRXPfwh7465505tW2q3hqpOOyMmXXAbua8fVn/hklXxVnCB0WQFLHIL/Dm
UyJnRy9vyKkt5QGzAhLMDWtRn/bejIJ8jzB8WFjf+luRpR/GdvPjy8Q7/0tY6By4
Ua67nlHTFkHJ7iytE1ShbNHoe1BtsuEdwu5BmNHDkixpUAFvm5MoC0Xvib4yvAVj
1+r/U9w5AFGMoNZrYO4Ckg3ZKCHwNMvI0sD0zHnW36jGV4yoNBQ+Uc3bBaxZKoy9
DxLIWT+066UqD2V3pIcObQT8xVQybxIHzoxFABEBAAH/AwMCB5I8JHaajnJgZ10f
dxEBzvlTmxBq2x4WGVvfhV6vks1CAu+KO6pGxaskJG/rNIXBrDv5g4dO3hIjaDQX
tOuyAp3WB+b06EnCRuXsUBQcDPoRZ8SfNlUkyNCEh9M3sDHng9nfVbGd5jjpFHiU
1Uw1OOGNd6TZLrmTzwqTpMoKs/cGw5GnT1LOsAy57oG0UuF9KVeknkOApN6K3Mez
VX6LiheaS8izV4Vfgr4pZn+GrL97iV41CITxPDOqL0AJvXmzAzT95324Bjo6+WAW
q3geDzMgd0udO4n/DQ1NSpBz1Wjvd9f60RDxfavpYKntEyy074ZbUAEGFE5CQhOy
r4JQQcXGzkkRK65/lkVbDsUAe2ICH2WQ5KEHe1gNEJyBQZr/s1XK0xO7B3EwPzDT
qdgN47H0DA7pXAbb3QxVxNTVx8HZkzxySJjmjrNvOC3VqKtDkQwY4dIDQ+R2ArrQ
7h9F/XdUAAFcw8XdtISl5XHWH13l3ZQb5DxpMOtjazLnFu5P00Drf9NyftO6eAY8
Xph+T1Oq24HXIO91zKsjpaV4Ru3U/5cxP8jp3rBLHkhWFnQrV6mVNuXeXPHYzMhj
LVxeTIoY/ELuM17fouyOEMhNLtDX3JZ5AXLctP9BEr5mwnjIJMmmB2yOOXMKZYeL
UO0pTBzgTI4BzlEpCcUQ2G8GAbpSN2f6R9trVgWNaX+hnC5GBv2yMIFfeKMIasS1
soGP5C6NPCBJDfd5iztfz8YLYwoaIb9d6HsQ06Xeylb/5cwpjehC4iL9OFI4emaJ
Ghrf/MeFfQVDdhM2MULaiJGMrWCvJNy+VMQfiX11/5iFtWgM6FK9D8yEdj2L7PEH
t/WzujPxWTS/AtnI7xnNhc7sb82LRsV7R2US7XTLDbQYc2FicmkuYXJzbGFuQHZl
cnNhdGEuY29tiQEcBBABAgAGBQJZ+d80AAoJEGHYYRGYWV281UkH/2wW6vqlqRzT
G2WFGAJEf3SoqYiEb5rJvA0aI10izRla2hzWV45F8xafmQEQN6ncU8k3cFIVdXw8
4Jq4NBlkIVngpbE7I1WqhiP5snFmJhdlpGjTdUvtKnSZNT/qMrdsyBwrOJ9SkmTO
NMG76HYXOGYJV0wxMh9PyABx+IzIHR/jdZ/5wDxhv76O/cV5oLcX/TK6UAjuQchO
drGAQFcbiwOlXv1wz8x4LrchcPgd2c5l9elozFLlDSKtukAnRIpgcNr71mv36/xF
bIICDu7Y9DBejbH0JPwumR3M6L4tVPAvgH1jcVzW28yF/qHrtfIoY+o1H/e7PF1v
XHfN4TUbhDg=
=sYfZ
-----END PGP PRIVATE KEY BLOCK-----
", Encoding.UTF8);
                }

                if (!File.Exists(_pgpSignatureKey))
                {
                    Directory.CreateDirectory(_pgpSignatureKey.Replace(Path.GetFileName(_pgpSignatureKey), ""));
                    File.WriteAllText(_pgpSignatureKey, @"-----BEGIN PGP PUBLIC KEY BLOCK-----
Version: BCPG C# v1.6.1.0

mQENBFn53zQBCADMlxdkpJRay9J1lA2Xc6fsyM0A7CRzcY5TCuU31ZROV0s8Z+97
hpwAdRzRXPfwh7465505tW2q3hqpOOyMmXXAbua8fVn/hklXxVnCB0WQFLHIL/Dm
UyJnRy9vyKkt5QGzAhLMDWtRn/bejIJ8jzB8WFjf+luRpR/GdvPjy8Q7/0tY6By4
Ua67nlHTFkHJ7iytE1ShbNHoe1BtsuEdwu5BmNHDkixpUAFvm5MoC0Xvib4yvAVj
1+r/U9w5AFGMoNZrYO4Ckg3ZKCHwNMvI0sD0zHnW36jGV4yoNBQ+Uc3bBaxZKoy9
DxLIWT+066UqD2V3pIcObQT8xVQybxIHzoxFABEBAAG0GHNhYnJpLmFyc2xhbkB2
ZXJzYXRhLmNvbYkBHAQQAQIABgUCWfnfNAAKCRBh2GERmFldvNVJB/9sFur6pakc
0xtlhRgCRH90qKmIhG+aybwNGiNdIs0ZWtoc1leORfMWn5kBEDep3FPJN3BSFXV8
POCauDQZZCFZ4KWxOyNVqoYj+bJxZiYXZaRo03VL7Sp0mTU/6jK3bMgcKzifUpJk
zjTBu+h2FzhmCVdMMTIfT8gAcfiMyB0f43Wf+cA8Yb++jv3FeaC3F/0yulAI7kHI
TnaxgEBXG4sDpV79cM/MeC63IXD4HdnOZfXpaMxS5Q0irbpAJ0SKYHDa+9Zr9+v8
RWyCAg7u2PQwXo2x9CT8LpkdzOi+LVTwL4B9Y3Fc1tvMhf6h67XyKGPqNR/3uzxd
b1x3zeE1G4Q4
=Yf0P
-----END PGP PUBLIC KEY BLOCK-----
", Encoding.UTF8);
                }

            }
        }

        /*
        private static TaskContext CreateContext()
        {
            return new TaskContext
            {
                EnvironmentId = _clientConfiguration.EnvironmentID, // Dev = 1, Qc = 2, Ua = 12, Production = 13
                BillingAdminConnection = Utility.BillingAdminDEV,
                Client = "Texpo",
                ClientName = "Texpo",
                Namespace = "Texpo",
                Abbreviation = "TXP",
                ClientId = 22,
                ClientConnection = _clientConfiguration.ConnectionCsr,
                MarketConnection = _clientConfiguration.ConnectionMarket,
                TDSPConnection = _clientConfiguration.ConnectionTdsp,
                TaxConnection = "",
                Logger = new ConsoleLogger()
            };
        }
        */

        public static void Main(string[] args)
        {      
            _clientConfig = ClientConfiguration.GetClientConfiguration(Clients.Texpo, Stages.UserAcceptance, TransactionMode.Enlist);
            _appConfig = ClientConfiguration.SetConfigurationContext(_clientConfig);

            #region old Cases
            //TransactionManager.DistributedTransactionStarted += delegate (object sender, TransactionEventArgs e)
            //{
            //    _logger.Info("Distributed Transaction Started");
            //};

            //ExecuteExport();

            //ExecuteProcessTransactionRequests();
            //GenerateSimpleMarketTransactionEvaluationEvents();
            //ProcessEvents();

            /*
			CalculateConsumptionDueDatesTask myTask = new CalculateConsumptionDueDatesTask();

            myTask.Initialize(CreateContext());

            //execute Maintenance.CalculateConsumptionDueDates
            myTask.Execute();


            //SimulateLetterGeneration("360533");
            ProcessEvents();

            string dirToProcess = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "MockData");
            Directory.EnumerateFiles(dirToProcess, "*.xls", SearchOption.AllDirectories).ForEach(
                filename =>
                {
                    SimulateImportMassEnrollment(filename);
                }
            );

            SimulateWSEnrollment();

            var notification = Notification.NewNotification();

            var emailParams = new Hashtable
            {
                { "FromEmail", "customer.care@southwestpl.com" },
                { "ToEmail", "ytnhome@gmail.com" },
                { "CustID", 90013 },
                { "InvoiceID", 7333859 },
                { "LOGO", "https://csr.texpobilling.com/Clients/TXP/Images/SWPL-Logo.gif" },
                { "CustName", "Young Nguyen" },
                { "DivisionName", "Southwest Power and Light" },
                { "DivisionPhoneNumber", "1-866-941-SWPL (7975)" }
            };

            notification.SendEmailJob(37, emailParams);

            Simulate_AESCIS_8679();
            
            Simulate_AESCIS_11082();
			*/
            #endregion

            var resp = "Y";
            while ("Y".Equals(resp, StringComparison.InvariantCultureIgnoreCase))
            {
                Simulate_AESCIS_20385(372012);

                Console.WriteLine("do you want to Repeat call");
                resp = Console.ReadLine().Trim();
            }
            _logger.Info("Debug session has ended");
            Console.ReadLine();
        }

        private static void Simulate_AESCIS_20385(string custId)
        {
            var prodConnectionString = _appConfig.ConnectionCsr
                .Replace("daes_", "paes_")
                .Replace("SGISUSEUAV01.aesua.local", "SGISUSEPRV01.aesprod.local");
            
            var maxTransactionDate = DB.ReadSingleValue<DateTime>($"SELECT MAX(TransactionDate) FROM CustomerTransactionRequest WHERE CustId = {custId} AND TransactionType='867'", prodConnectionString).ToString("yyyy-MM-ddT00:00:00");



            DB.ImportQueryResultsFromProduction(
                string.Format(MockData.SqlScripts.CustomerExportScript, custId, 10),
                _appConfig.ConnectionCsr,
                _appDir,
                (xmlFileName, connectionString) =>
                {
                    DB.ExecuteQuery("DISABLE TRIGGER ALL ON PaymentDetail;", connectionString);

                    var doc = new XmlDocument();
                    doc.Load(xmlFileName);

                    XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
                    var rootNode = doc.SelectSingleNode("root", nsMgr);
                    if (rootNode == null)
                    {
                        _logger.Error("Root element not found, can't process the xml");
                        return;
                    }

                    // do necessary changes here

                    ////delete latest RateTransitions first Rollover then ChangeProductRequest
                    //var lastRateTransition = doc.SelectSingleNode("(//RateTransition)[last()]", nsMgr);
                    //while (lastRateTransition?.Attributes?["RolloverFlag"]?.Value == "1")
                    //{
                    //    rootNode.RemoveChild(lastRateTransition);
                    //    var rtId = lastRateTransition.Attributes["RateTransitionID"].Value;
                    //    var ccRT = doc.SelectSingleNode($"(//ClientCustomer.RateTransition[@RateTransitionID='{rtId}'])", nsMgr);
                    //    if (ccRT != null)
                    //    {
                    //        rootNode.RemoveChild(ccRT);
                    //    }
                    //    lastRateTransition = doc.SelectSingleNode("(//RateTransition)[last()]", nsMgr);
                    //}

                    ////delete the last RT
                    //lastRateTransition = doc.SelectSingleNode("(//RateTransition)[last()]", nsMgr);
                    //if (lastRateTransition == null)
                    //{
                    //    _logger.Error("No RT left for deleting, something went wrong!");
                    //    return;
                    //}
                    //rootNode.RemoveChild(lastRateTransition);


                    //lastRateTransition = doc.SelectSingleNode("(//RateTransition)[last()]", nsMgr);
                    //if (lastRateTransition == null)
                    //{
                    //    _logger.Error("No RT left for updating, something went wrong!");
                    //    return;
                    //}
                    //var lastRTDate = DateTime.Parse(lastRateTransition.Attributes["SwitchDate"].Value);

                    //var lastChangeProductRequest = doc.SelectSingleNode("(//ClientCustomer.ChangeProductRequest)[last()]", nsMgr);
                    //if (lastChangeProductRequest == null)
                    //{
                    //    _logger.Error("Could not find ChangeProduct request to simulate");
                    //    return;
                    //}
                    //lastChangeProductRequest.Attributes["ContractId"].Value = "0";

                    //var requestedDate = DateTime.Today.AddDays(-7).ToString("yyyy-MM-ddT00:00:00");
                    //lastChangeProductRequest.Attributes["ProductEffectiveDate"].Value = requestedDate;
                    //lastChangeProductRequest.Attributes["RequestedContractStartDate"].Value = requestedDate;
                    //lastChangeProductRequest.Attributes["ActualContractStartDate"].Value = requestedDate;

                    ////set bufferDate today so it can be run today
                    //lastChangeProductRequest.Attributes["BufferDate"].Value = DateTime.Today.ToString("yyyy-MM-ddT00:00:00");


                    //var lastCustomerTransactionRequest = doc.SelectSingleNode("(//CustomerTransactionRequest)[last()]", nsMgr);
                    //while (lastCustomerTransactionRequest.Attributes["TransactionType"].Value == "814" &&
                    //lastCustomerTransactionRequest.Attributes["ActionCode"].Value == "C" &&
                    //DateTime.Parse(lastCustomerTransactionRequest.Attributes["TransactionDate"].Value) > lastRTDate)
                    //{
                    //    rootNode.RemoveChild(lastCustomerTransactionRequest);
                    //    lastCustomerTransactionRequest = doc.SelectSingleNode("(//CustomerTransactionRequest)[last()]", nsMgr);
                    //}

                    ////lastRateTransition.Attributes["EndDate"].Value = lastRTDate
                    ////    .AddMonths(int.Parse(lastChangeProductRequest.Attributes["ContractTermsInMonths"].Value))
                    ////    .ToString("yyyy-MM-ddT00:00:00");


                    //lastRateTransition.Attributes["EndDate"].Value = DateTime.Today.AddDays(-1).ToString("yyyy-MM-ddT00:00:00");

                    //var customer = doc.SelectSingleNode("(//Customer)[last()]", nsMgr);
                    //var lastcprId = lastChangeProductRequest.Attributes["ChangeProductRequestID"].Value;
                    //var activeChangeProductRequest = doc.SelectSingleNode($"(//ClientCustomer.ChangeProductRequest)[@ChangeProductRequestID < '{lastcprId}'][last()]", nsMgr);
                    //var activeContractId = activeChangeProductRequest.Attributes["ContractId"].Value;
                    //customer.Attributes["ContractID"].Value = activeContractId;
                    //var activeContract = doc.SelectSingleNode($"(//Contract)[@ContractID = '{activeContractId}']", nsMgr);
                    //customer.Attributes["ContractEndDate"].Value = activeContract.Attributes["EndDate"].Value;

                    doc.Save(xmlFileName);
                },
                connectionString =>
                {
                    DB.ExecuteQuery("ENABLE TRIGGER ALL ON ChangeRequest;", connectionString);

                    // make new payment here to trigger EventEvalutionQueue Insert

                    // delete latest CTR
                    var sql = $"DELETE FROM CustomerTransactionRequest WHERE CustId = {custId}  AND TransactionType='650' AND TransactionDate>'{maxTransactionDate}'";
                    DB.ExecuteQuery(sql, connectionString);

                }
            );


            
            //EventEvaluationQueue
            GenerateEvents(new List<int> { 27 });
        }

        private static void Simulate_AESCIS_11082()
        {
            DB.ImportFiles(_mockDataDir, "CnclCons", _appConfig.ConnectionCsr);
            var customerId = 13502;
            var invoiceBeginDate = new DateTime(2018, 1, 12);
            var invoicEndDate = new DateTime(2018, 1, 22);
            var invoiceDate = new DateTime(2018,1,23);
            //var consId = 5816763;
            //var sql = $"UPDATE Consumption SET Processed='N', ProcessDate = NULL, DoNotProcess = 0, RequestId = NULL, ValidateFlag = 0, ValidatedDate = NULL WHERE ConsId = {consId}";
            //DB.ExecuteQuery(sql, _appConfig.ConnectionCsr);

            // delete consumptions
            var header_key = 5466809;
            var sql = $@"
DECLARE @RequestId INT = (select RequestId from CustomerTransactionRequest where TransactionType = '867' AND Direction = 1 AND SourceId = {header_key})
DECLARE @InvoiceId INT = (select InvoiceId from Consumption WHERE Source = 'NonIntervalDetail' AND RequestId = @RequestId)
DECLARE @BatchId INT = (select BatchId from BatchDetail WHERE InvoiceId = @InvoiceId)

IF (ISNULL(@BatchId,0)>0)
BEGIN
DELETE FROM Batch WHERE BatchId = @BatchId
DELETE FROM BatchDetail WHERE BatchId = @BatchId
DELETE FROM InvoiceTax WHERE InvDetId IN (SELECT InvDetId FROM InvoiceDetail WHERE InvoiceId = @InvoiceId)
DELETE FROM InvoiceDetail WHERE InvoiceId = @InvoiceId
DELETE FROM InvoiceXML WHERE InvoiceId = @InvoiceId
DELETE FROM Invoice WHERE InvoiceId = @InvoiceId
END

DELETE FROM ConsumptionDetail WHERE ConsId IN (SELECT ConsId FROM Consumption WHERE Source = 'NonIntervalDetail' AND RequestId = @RequestId)
DELETE FROM Consumption WHERE Source = 'NonIntervalDetail' AND RequestId = @RequestId
DELETE FROM CustomerTransactionRequest WHERE RequestId = @RequestId 

";
            DB.ExecuteQuery(sql, _appConfig.ConnectionCsr);

            SimulateImportConsumption();

            //create invoice
            var batchId = CreateInvoiceBatch(customerId, invoiceBeginDate, invoicEndDate, invoiceDate);
            var invoiceId = SimulateCreateInvoice(customerId, invoiceBeginDate, invoicEndDate, invoiceDate);
            UpdateBatchDetail(invoiceId, batchId);


            //SimulateImportTransactionQueue();

            //GenerateEvents();
            //ProcessEvents();
            //GenerateEvents();
            //ProcessEvents();
        }

        private static int SimulateCreateInvoice(int customerId, DateTime beginDate, DateTime endDate,
            DateTime invoiceDate)
        {
            var invoice = new CIS.Clients.Texpo.Invoice(_appConfig.ConnectionCsr)
            {
                ConnectionAdmin = _clientConfig.ConnectionBillingAdmin,
                ClientID = Clients.Texpo.Id(),
                UserId = 370379
            };

            if (invoice.GenerateStandardInvoice(customerId, beginDate, endDate, invoiceDate))
            {
                return invoice.InvoiceID;
            }

            throw new Exception("Error occurred when generating invoice");
        }

        private static int CreateInvoiceBatch(int customerId, DateTime beginDate, DateTime endDate,
            DateTime invoiceDate)
        {
            var arrParms = new SqlParameter[5];
            arrParms[0] = new SqlParameter("@CustID", SqlDbType.Int)
            {
                Value = customerId
            };
            arrParms[1] = new SqlParameter("@RangeBeginDate", SqlDbType.DateTime)
            {
                Value = beginDate
            };
            arrParms[2] = new SqlParameter("@RangeEndDate", SqlDbType.DateTime)
            {
                Value = endDate
            };
            arrParms[3] = new SqlParameter("@InvoiceDate", SqlDbType.DateTime)
            {
                Value = invoiceDate
            };
            arrParms[4] = new SqlParameter("@BatchID", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            SqlHelper.ExecuteDataset(_appConfig.ConnectionCsr, CommandType.StoredProcedure,
                "cspBatchCreateByCustomer", arrParms);
            return Util.ToInt32(arrParms[4].Value);
        }

        private static void UpdateBatchDetail(int invoiceId, int batchId)
        {
            var arrParms = new SqlParameter[2];
            arrParms[0] = new SqlParameter("@BatchID", SqlDbType.Int);
            arrParms[0].Value = batchId;
            arrParms[1] = new SqlParameter("@InvoiceID", SqlDbType.Int);
            arrParms[1].Value = invoiceId;
            SqlHelper.ExecuteNonQuery(_appConfig.ConnectionCsr, CommandType.StoredProcedure, "cspBatchDetailUpdateInvoiceByCustomer", arrParms);
        }

        private static void Simulate_AESCIS_8679()
        {
            var updateRunHoursQueries = new List<string>();
            var restoreRunHousrsQueries = new List<string>();

            //get run hours of cspEmailJobDeclinedPaymentNotice
            var rows = DB.ReadRows("SELECT * FROM EmailJob WHERE ResultsProcedure='cspEmailJobDeclinedPaymentNotice' AND IsActive = 1", _appConfig.ConnectionCsr);
            foreach (DataRow row in rows)
            {
                updateRunHoursQueries.Add($"UPDATE EmailJob SET RunOnHour = {DateTime.Now.Hour}, LastRunDate = '2017-02-01 06:00:00' WHERE EmailJobID = {row["EmailJobId"]} ");
                restoreRunHousrsQueries.Add($"UPDATE EmailJob SET RunOnHour = {row["RunOnHour"]} WHERE EmailJobID = {row["EmailJobId"]} ");
            }

            _logger.Info(string.Join(";\n", restoreRunHousrsQueries));

            string dirToProcess = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "MockData");
            DB.ImportFiles(dirToProcess, "AESCIS-8679", _appConfig.ConnectionCsr);
            
            try
            {

                //modify RunOnHour to this time hour and point last run hours to morning of the day of Payments
                SqlHelper.ExecuteNonQuery(_appConfig.ConnectionCsr, CommandType.Text, string.Join(";\n", updateRunHoursQueries));

                //execute EmailJob
                var emailJob = new CIS.Clients.Texpo.EmailJob(_appConfig.ConnectionCsr);
                emailJob.Process();
            }
            finally
            {
                //restore RunOnHour
                SqlHelper.ExecuteNonQuery(_appConfig.ConnectionCsr, CommandType.Text, string.Join(";\n", restoreRunHousrsQueries));
            }
        }

        private static void SimulateWSEnrollment()
        {
            var client = new CIS.Web.Services.Clients.Texpo.Enrollment();
            //var newCustomer = client.EnrollNonTexasCustomer(CustomerTypeOptions.RESIDENTIAL,
            //    "Kadir",
            //    "",
            //    "McPhearson",
            //    "Partridgespring St. 407",
            //    "",
            //    "Bellrock",
            //    "CA",
            //    "06450",
            //    332397,
            //    24,
            //    "kadir@kadir.com",
            //    DateTime.Parse("1982-7-19"),
            //    "76357724",
            //    "510-657-8682",
            //    "",
            //    "",
            //    true,
            //    DateTime.Parse("2017-12-18"),
            //    "Aurea Commercials",
            //    "f776a9ae-7b94-4bd8-9260-ffebe376cac6",
            //    11,
            //    0,
            //    null,
            //    null,
            //    null,
            //    null,
            //    null,
            //    null,
            //    null);

            //var methodInfo = client.GetType().GetMethod("PerformEnrollCustomer", BindingFlags.Instance | BindingFlags.NonPublic);
            //methodInfo.Invoke(client, new object[] {
            //    CustomerTypeOptions.RESIDENTIAL,
            //    "Kadir",
            //    "",
            //    "McPhearson",
            //    "Partridgespring St. 407",
            //    "",
            //    "Bellrock",
            //    "CA",
            //    "06450",
            //    332397,
            //    24,
            //    "kadir@kadir.com",
            //    DateTime.Parse("1982-7-19"),
            //    "76357724",
            //    "510-657-8682",
            //    "",
            //    "",
            //    true,
            //    DateTime.Parse("2017-12-18"),
            //    "Aurea Commercials",
            //    "f776a9ae-7b94-4bd8-9260-ffebe376cac6",
            //    11,
            //    0,
            //    null,
            //    null});
        }

        private static void SimulateImportConsumption()
        {
            try
            {
                bool bSuccess = false;
                /*CISRFC-783 Implements Market Gap*/
                CIS.Import.Billing.ConsumptionImportService consImpService = CreateConsumptionImportService();
                if (consImpService != null)
                {
                    bSuccess = consImpService.ImportConsumption();
                }

                
                CIS.Import.Billing.DailyConsumption dc = CreateDailyConsumptionImporter();
                if (dc != null)
                {
                    bSuccess = dc.Import();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex,"Error Ocurred when importing consumptions");
            }

        }

        private static CIS.Import.Billing.ConsumptionImportService CreateConsumptionImportService()
        {
            return new CIS.Import.Billing.ConsumptionImportService(_appConfig.ConnectionMarket, _appConfig.ConnectionCsr, _logger);
        }

        private static CIS.Import.Billing.DailyConsumption CreateDailyConsumptionImporter()
        {
            //If DailyRead Process is enabled, we will import the Daily Reads as consumption
            ProcessConfigurationDS.ProcessConfigurationPivot pc = ProcessConfigurationBC.LoadPivot(_clientConfig.ConnectionBillingAdmin, _clientConfig.Client, "DailyRead");
            bool isImportDailyReadEnabled = Util.GetBool(pc, "IsEnabled");
            int batchSize = Util.GetInt32(pc, "BatchSize", 1000);

            if (isImportDailyReadEnabled)
                return new CIS.Import.Billing.DailyConsumption(_appConfig.ConnectionMarket, _appConfig.ConnectionCsr, batchSize);

            return null;
        }

        private static void SimulateImportTransactionQueue()
        {
            var queue = new CIS.Import.Billing.Transaction.Queue(_clientConfig.Client, _appConfig.ConnectionMarket,
                _appConfig.ConnectionCsr, _appConfig.ConnectionTdsp, _clientConfig.ConnectionBillingAdmin, _logger);
            queue.Import();
        }

        private static void SimulateImportMassEnrollment(string fileName)
        {
            //TexpoWS.EnrollmentSoapClient cl = new EnrollmentSoapClient();
            //cl.EnrollNonTexasCustomer()
            DeleteFileImportStatus(fileName);
            var massEnrollImporter = new MassEnrollImporter(_clientConfig.ConnectionBillingAdmin, _appConfig.ConnectionCsr, _logger);
            var data = massEnrollImporter.ConvertToEnrollRows(massEnrollImporter.CheckEnrollRows(massEnrollImporter.GetDataSetFromMassEnrollExcelFile(fileName), fileName)).ToArray();
            var totalRecords = data.Count();

            _logger.Info($"Texpo MassEnroll. {totalRecords} record(s) has/have been sucessfuly read. From file: {fileName}.");

            foreach (var rec in data)
            {
                CopyRateAndProduct(rec.RateId);
            }

            var bSuccess = massEnrollImporter.ImportFile(fileName);
            _logger.Info($"massEnrollImporter has return with {bSuccess}");

        }

        private static void DeleteFileImportStatus(string fileName)
        {
            var fileNameToInsert = Path.GetFileName(fileName);
            string checkSum;

            using (var f = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var reader = new StreamReader(f))
                {
                    var cry = new CIS.Library.Cryptography.Hash(CIS.Library.Cryptography.Hash.ServiceProviderOptions.MD5);
                    checkSum = cry.Encrypt(reader.ReadToEnd(), "arc3iWY=");
                }
            }

            _logger.Info($"going to delete old import status for '{fileNameToInsert}' and chksum '{checkSum}'");

            var sql = $"DELETE FROM dbo.tblFile WHERE [FileName] = '{fileNameToInsert}' OR [FileHash] = '{checkSum}'";
            DB.ExecuteQuery(sql, _appConfig.ConnectionCsr);
        }

        private static void CopyRateAndProduct(string rateId)
        {
            string sql=$@"

PRINT 'Copy Rate'
SET IDENTITY_INSERT daes_Texpo..Rate ON

INSERT INTO daes_Texpo..Rate ([RateID], [CSPID], [RateCode], [RateDesc], [EffectiveDate], [ExpirationDate], [RateType], [PlanType], [IsMajority], [TemplateFlag], [CreateDate], [UserID], [RatePackageName], [CustType], [ServiceType], [DivisionCode], [LDCCode], [ConsUnitId], [ActiveFlag], [LDCRateCode])
SELECT  [RateID], [CSPID], [RateCode], [RateDesc], [EffectiveDate], [ExpirationDate], [RateType], [PlanType], [IsMajority], [TemplateFlag], [CreateDate], [UserID], [RatePackageName], [CustType], [ServiceType], [DivisionCode], [LDCCode], [ConsUnitId], [ActiveFlag], [LDCRateCode]
FROM  saes_Texpo..Rate WHERE RateID = '{rateId}'
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Rate WHERE RateID = '{rateId}')

SET IDENTITY_INSERT daes_Texpo..Rate OFF

PRINT 'Copy RateDetail'
SET IDENTITY_INSERT daes_Texpo..RateDetail ON

INSERT INTO daes_Texpo..RateDetail ([RateDetID], [RateID], [CategoryID], [RateTypeID], [ConsUnitID], [RateDescID], [EffectiveDate], [ExpirationDate], [RateAmt], [RateAmt2], [RateAmt3], [FixedAdder], [MinDetAmt], [MaxDetAmt], [GLAcct], [RangeLower], [RangeUpper], [CustType], [Graduated], [Progressive], [AmountCap], [MaxRateAmt], [MinRateAmt], [CategoryRollup], [Taxable], [ChargeType], [MiscData1], [FixedCapRate], [ScaleFactor1], [ScaleFactor2], [TemplateRateDetID], [Margin], [UsageClassId], [LegacyRateDetailId], [Active], [StatusID], [Building], [ServiceTypeID], [TaxCategoryID], [UtilityID], [UtilityInvoiceTemplateDetailID], [RateVariableTypeId], [MinDays], [MaxDays], [MeterMultiplierFlag], [CreateDate], [BlockPriceIndicator], [RateTransitionId], [BlendRatio], [ContractVolumeID], [CreatedByUserId], [ModifiedByUserId], [ModifiedDate], [TOUTemplateID], [TOUTemplateRegisterID], [TOUTemplateRegisterName])
SELECT  [RateDetID], [RateID], [CategoryID], [RateTypeID], [ConsUnitID], [RateDescID], [EffectiveDate], [ExpirationDate], [RateAmt], [RateAmt2], [RateAmt3], [FixedAdder], [MinDetAmt], [MaxDetAmt], [GLAcct], [RangeLower], [RangeUpper], [CustType], [Graduated], [Progressive], [AmountCap], [MaxRateAmt], [MinRateAmt], [CategoryRollup], [Taxable], [ChargeType], [MiscData1], [FixedCapRate], [ScaleFactor1], [ScaleFactor2], [TemplateRateDetID], [Margin], [UsageClassId], [LegacyRateDetailId], [Active], [StatusID], [Building], [ServiceTypeID], [TaxCategoryID], [UtilityID], [UtilityInvoiceTemplateDetailID], [RateVariableTypeId], [MinDays], [MaxDays], [MeterMultiplierFlag], [CreateDate], [BlockPriceIndicator], [RateTransitionId], [BlendRatio], [ContractVolumeID], [CreatedByUserId], [ModifiedByUserId], [ModifiedDate], [TOUTemplateID], [TOUTemplateRegisterID], [TOUTemplateRegisterName]
FROM  saes_Texpo..RateDetail WHERE RateID = '{rateId}'
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..RateDetail WHERE RateID = '{rateId}')

SET IDENTITY_INSERT daes_Texpo..RateDetail OFF

PRINT 'Copy Product'
SET IDENTITY_INSERT daes_Texpo..Product ON

INSERT INTO daes_Texpo..Product ([ProductID], [RateID], [LDCCode], [PlanType], [TDSPTemplateID], [Description], [BeginDate], [EndDate], [CustType], [Graduated], [RangeTier1], [RangeTier2], [SortOrder], [ActiveFlag], [Uplift], [CSATDSPTemplateID], [CAATDSPTemplateID], [PriceDescription], [MarketingCode], [RateTypeID], [Default], [ConsUnitID], [DivisionCode], [ServiceType], [CSPId], [TermsId], [RolloverProductId], [CommissionId], [CommissionAmt], [CancelFeeId], [MonthlyChargeId], [ProductCode], [RatePackageId], [ProductName], [TermDate], [DiscountTypeId], [DiscountAmount], [ProductZoneID], [IsGreen], [IsBestChoice], [ActiveEnrollmentFlag], [DepositAmount], [CreditScoreThreshold], [Incentives])
SELECT  [ProductID], [RateID], [LDCCode], [PlanType], [TDSPTemplateID], [Description], [BeginDate], [EndDate], [CustType], [Graduated], [RangeTier1], [RangeTier2], [SortOrder], [ActiveFlag], [Uplift], [CSATDSPTemplateID], [CAATDSPTemplateID], [PriceDescription], [MarketingCode], [RateTypeID], [Default], [ConsUnitID], [DivisionCode], [ServiceType], [CSPId], [TermsId], [RolloverProductId], [CommissionId], [CommissionAmt], [CancelFeeId], [MonthlyChargeId], [ProductCode], [RatePackageId], [ProductName], [TermDate], [DiscountTypeId], [DiscountAmount], [ProductZoneID], [IsGreen], [IsBestChoice], [ActiveEnrollmentFlag], [DepositAmount], [CreditScoreThreshold], [Incentives]
FROM  saes_Texpo..Product WHERE RateId = '{rateId}'
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Product WHERE RateId = '{rateId}')

SET IDENTITY_INSERT daes_Texpo..Product OFF

";
            DB.ExecuteQuery(sql, _appConfig.ConnectionCsr);
        }

        private static void SimulateLetterGeneration(string custNo)
        {
            CopyCustomer(custNo);
            MakeCustomerEligibleForDisconnectLetter(custNo);
            GenerateEvents();
        }

        private static void CopyCustomer(string custNo)
        {
            string sql = $@"USE daes_Texpo
DECLARE @ClientID AS INT = (SELECT ClientID FROM daes_BillingAdmin..Client WHERE Client='TXP')
DECLARE @CustNo AS VARCHAR(20) = '{custNo}'
DECLARE @CustID AS INT = (SELECT CustID FROM saes_Texpo..Customer WHERE CustNo = @CustNo)


PRINT 'BEGIN Copy Customer'

PRINT 'Copy Addresses'
SET IDENTITY_INSERT daes_Texpo..Address ON

INSERT INTO daes_Texpo..Address ([AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion])
SELECT [AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion]
FROM  saes_Texpo..Address WHERE AddrID IN (
	SELECT SiteAddrId FROM saes_Texpo..Customer WHERE CustID = @CustID
)
AND NOT EXISTS (
SELECT 1 FROM daes_Texpo..Address WHERE AddrID IN (
	SELECT SiteAddrId FROM saes_Texpo..Customer WHERE CustID = @CustID
	)
)

INSERT INTO daes_Texpo..Address ([AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion])
SELECT [AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion]
FROM  saes_Texpo..Address WHERE AddrID IN (
	SELECT MailAddrId FROM saes_Texpo..Customer WHERE CustID = @CustID
)
AND NOT EXISTS (
SELECT 1 FROM daes_Texpo..Address WHERE AddrID IN (
	SELECT MailAddrId FROM saes_Texpo..Customer WHERE CustID = @CustID
	)
)

INSERT INTO daes_Texpo..Address ([AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion])
SELECT [AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion]
FROM  saes_Texpo..Address WHERE AddrID IN (
	SELECT CorrAddrId FROM saes_Texpo..Customer WHERE CustID = @CustID
)
AND NOT EXISTS (
SELECT 1 FROM daes_Texpo..Address WHERE AddrID IN (
	SELECT CorrAddrId FROM saes_Texpo..Customer WHERE CustID = @CustID
	)
)

INSERT INTO daes_Texpo..Address ([AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion])
SELECT [AddrID], [ValidationStatusID], [AttnMS], [Addr1], [Addr2], [City], [State], [Zip], [Zip4], [DPBC], [CityID], [CountyID], [County], [HomePhone], [WorkPhone], [FaxPhone], [OtherPhone], [Email], [ESIID], [GeoCode], [Status], [DeliveryPointCode], [CreateDate], [PhoneExtension], [OtherExtension], [FaxExtension], [TaxingDistrict], [TaxInCity], [Migr_Enrollid], [CchVersion]
FROM  saes_Texpo..Address WHERE AddrID IN (
	SELECT AddrId FROM saes_Texpo..Premise WHERE CustID = @CustID
)
AND NOT EXISTS (
SELECT 1 FROM daes_Texpo..Address WHERE AddrID IN (
	SELECT AddrId FROM saes_Texpo..Premise WHERE CustID = @CustID
	)
)

SET IDENTITY_INSERT daes_Texpo..Address OFF

PRINT 'Copy Rate'
SET IDENTITY_INSERT daes_Texpo..Rate ON

INSERT INTO daes_Texpo..Rate ([RateID], [CSPID], [RateCode], [RateDesc], [EffectiveDate], [ExpirationDate], [RateType], [PlanType], [IsMajority], [TemplateFlag], [CreateDate], [UserID], [RatePackageName], [CustType], [ServiceType], [DivisionCode], [LDCCode], [ConsUnitId], [ActiveFlag], [LDCRateCode])
SELECT  [RateID], [CSPID], [RateCode], [RateDesc], [EffectiveDate], [ExpirationDate], [RateType], [PlanType], [IsMajority], [TemplateFlag], [CreateDate], [UserID], [RatePackageName], [CustType], [ServiceType], [DivisionCode], [LDCCode], [ConsUnitId], [ActiveFlag], [LDCRateCode]
FROM  saes_Texpo..Rate WHERE RateID IN (
	SELECT RateID FROM saes_Texpo..Customer WHERE CustID = @CustID
)
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Rate WHERE RateID IN (
	SELECT RateID FROM saes_Texpo..Customer WHERE CustID = @CustID
	)
)

INSERT INTO daes_Texpo..Rate ([RateID], [CSPID], [RateCode], [RateDesc], [EffectiveDate], [ExpirationDate], [RateType], [PlanType], [IsMajority], [TemplateFlag], [CreateDate], [UserID], [RatePackageName], [CustType], [ServiceType], [DivisionCode], [LDCCode], [ConsUnitId], [ActiveFlag], [LDCRateCode])
SELECT  [RateID], [CSPID], [RateCode], [RateDesc], [EffectiveDate], [ExpirationDate], [RateType], [PlanType], [IsMajority], [TemplateFlag], [CreateDate], [UserID], [RatePackageName], [CustType], [ServiceType], [DivisionCode], [LDCCode], [ConsUnitId], [ActiveFlag], [LDCRateCode]
FROM  saes_Texpo..Rate WHERE RateID IN (
	SELECT RateID FROM saes_Texpo..Product WHERE ProductId IN (SELECT ProductID FROM saes_Texpo..Contract WHERE CustID = @CustID)
)
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Rate WHERE RateID IN (
	SELECT RateID FROM saes_Texpo..Product WHERE ProductId IN (SELECT ProductID FROM saes_Texpo..Contract WHERE CustID = @CustID)
	)
)
SET IDENTITY_INSERT daes_Texpo..Rate OFF

PRINT 'Copy Customer'
SET IDENTITY_INSERT daes_Texpo..Customer ON

INSERT INTO daes_Texpo..Customer ([CustID], [CSPID], [CSPCustID], [PropertyID], [PropertyCustID], [CustomerTypeID], [CustNo], [CustName], [LastName], [FirstName], [MidName], [CompanyName], [DBA], [FederalTaxID], [AcctsRecID], [DistributedAR], [ProductionCycle], [BillCycle], [RateID], [SiteAddrID], [MailAddrId], [CorrAddrID], [MailToSiteAddress], [BillCustID], [MasterCustID], [Master], [CustStatus], [BilledThru], [CSRStatus], [CustType], [Services], [FEIN], [DOB], [Taxable], [LateFees], [NoOfAccts], [ConsolidatedInv], [SummaryInv], [MsgID], [TDSPTemplateID], [TDSPGroupID], [LifeSupportIndictor], [LifeSupportStatus], [LifeSupportDate], [SpecialBenefitsPlan], [BillFormat], [PrintLayoutID], [CreditScore], [HitIndicator], [RequiredDeposit], [AccountManager], [EnrollmentAlias], [ContractID], [ContractTerm], [ContractStartDate], [ContractEndDate], [UserDefined1], [CreateDate], [RateChangeDate], [ConversionAccountNo], [PermitContactName], [CustomerPrivacy], [UsagePrivacy], [CompanyRegistrationNumber], [VATNumber], [AccountStatus], [AutoCreditAfterInvoiceFlag], [LidaDiscount], [DoNotDisconnect], [DDPlus1], [CsrImportDate], [DeliveryTypeID], [SpecialNeedsAddrID], [PORFlag], [PaymentModelId], [PowerOutageAddrId])
SELECT [CustID], [CSPID], [CSPCustID], [PropertyID], [PropertyCustID], [CustomerTypeID], [CustNo], [CustName], [LastName], [FirstName], [MidName], [CompanyName], [DBA], [FederalTaxID], [AcctsRecID], [DistributedAR], [ProductionCycle], [BillCycle], [RateID], [SiteAddrID], [MailAddrId], [CorrAddrID], [MailToSiteAddress], [BillCustID], [MasterCustID], [Master], [CustStatus], [BilledThru], [CSRStatus], [CustType], [Services], [FEIN], [DOB], [Taxable], [LateFees], [NoOfAccts], [ConsolidatedInv], [SummaryInv], [MsgID], [TDSPTemplateID], [TDSPGroupID], [LifeSupportIndictor], [LifeSupportStatus], [LifeSupportDate], [SpecialBenefitsPlan], [BillFormat], [PrintLayoutID], [CreditScore], [HitIndicator], [RequiredDeposit], [AccountManager], [EnrollmentAlias], [ContractID], [ContractTerm], [ContractStartDate], [ContractEndDate], [UserDefined1], [CreateDate], [RateChangeDate], [ConversionAccountNo], [PermitContactName], [CustomerPrivacy], [UsagePrivacy], [CompanyRegistrationNumber], [VATNumber], [AccountStatus], [AutoCreditAfterInvoiceFlag], [LidaDiscount], [DoNotDisconnect], [DDPlus1], [CsrImportDate], [DeliveryTypeID], [SpecialNeedsAddrID], [PORFlag], [PaymentModelId], [PowerOutageAddrId]
FROM  saes_Texpo..Customer WHERE CustID = @CustID
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Customer WHERE CustId = @CustID)
SET IDENTITY_INSERT daes_Texpo..Customer OFF

PRINT 'Copy Premise'
ALTER TABLE daes_Texpo..Customer NOCHECK CONSTRAINT ALL
ALTER TABLE daes_Texpo..Premise NOCHECK CONSTRAINT ALL
SET IDENTITY_INSERT daes_Texpo..Premise ON

INSERT INTO daes_Texpo..Premise ([PremID], [CustID], [CSPID], [AddrID], [TDSPTemplateID], [ServiceCycle], [TDSP], [TaxAssessment], [PremNo], [PremDesc], [PremStatus], [PremType], [LocationCode], [SpecialNeedsFlag], [SpecialNeedsStatus], [SpecialNeedsDate], [ReadingIncrement], [Metered], [Taxable], [BeginServiceDate], [EndServiceDate], [SourceLevel], [StatusID], [StatusDate], [CreateDate], [UnitID], [PropertyCommonID], [RateID], [DeleteFlag], [LBMPId], [PipelineId], [GasLossId], [GasPoolID], [LDCID], [DeliveryPoint], [ConsumptionBandIndex], [LastModifiedDate], [CreatedByID], [ModifiedByID], [BillingAccountNumber], [NameKey], [GasSupplyServiceOption], [IntervalUsageTypeId], [LDC_UnMeteredAcct], [OnSwitchHold], [SwitchHoldStartDate], [ConsumptionImportTypeId], [TDSPTemplateEffectiveDate], [AltPremNo], [ServiceDeliveryPoint], [UtilityContractID], [LidaDiscount], [GasCapacityAssignment], [CPAEnrollmentTypes], [SupplierPricingStructureNr], [SupplierGroupNumber], [IsTOU])
SELECT [PremID], [CustID], [CSPID], [AddrID], [TDSPTemplateID], [ServiceCycle], [TDSP], [TaxAssessment], [PremNo], [PremDesc], [PremStatus], [PremType], [LocationCode], [SpecialNeedsFlag], [SpecialNeedsStatus], [SpecialNeedsDate], [ReadingIncrement], [Metered], [Taxable], [BeginServiceDate], [EndServiceDate], [SourceLevel], [StatusID], [StatusDate], [CreateDate], [UnitID], [PropertyCommonID], [RateID], [DeleteFlag], [LBMPId], [PipelineId], [GasLossId], [GasPoolID], [LDCID], [DeliveryPoint], [ConsumptionBandIndex], [LastModifiedDate], [CreatedByID], [ModifiedByID], [BillingAccountNumber], [NameKey], [GasSupplyServiceOption], [IntervalUsageTypeId], [LDC_UnMeteredAcct], [OnSwitchHold], [SwitchHoldStartDate], [ConsumptionImportTypeId], [TDSPTemplateEffectiveDate], [AltPremNo], [ServiceDeliveryPoint], [UtilityContractID], [LidaDiscount], [GasCapacityAssignment], [CPAEnrollmentTypes], [SupplierPricingStructureNr], [SupplierGroupNumber], [IsTOU]
FROM  saes_Texpo..Premise WHERE CustID = @CustID
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Premise WHERE CustId = @CustID)
SET IDENTITY_INSERT daes_Texpo..Premise OFF
ALTER TABLE daes_Texpo..Premise WITH CHECK CHECK CONSTRAINT ALL

PRINT 'Copy Product'
SET IDENTITY_INSERT daes_Texpo..Product ON

INSERT INTO daes_Texpo..Product ([ProductID], [RateID], [LDCCode], [PlanType], [TDSPTemplateID], [Description], [BeginDate], [EndDate], [CustType], [Graduated], [RangeTier1], [RangeTier2], [SortOrder], [ActiveFlag], [Uplift], [CSATDSPTemplateID], [CAATDSPTemplateID], [PriceDescription], [MarketingCode], [RateTypeID], [Default], [ConsUnitID], [DivisionCode], [ServiceType], [CSPId], [TermsId], [RolloverProductId], [CommissionId], [CommissionAmt], [CancelFeeId], [MonthlyChargeId], [ProductCode], [RatePackageId], [ProductName], [TermDate], [DiscountTypeId], [DiscountAmount], [ProductZoneID], [IsGreen], [IsBestChoice], [ActiveEnrollmentFlag], [DepositAmount], [CreditScoreThreshold], [Incentives])
SELECT  [ProductID], [RateID], [LDCCode], [PlanType], [TDSPTemplateID], [Description], [BeginDate], [EndDate], [CustType], [Graduated], [RangeTier1], [RangeTier2], [SortOrder], [ActiveFlag], [Uplift], [CSATDSPTemplateID], [CAATDSPTemplateID], [PriceDescription], [MarketingCode], [RateTypeID], [Default], [ConsUnitID], [DivisionCode], [ServiceType], [CSPId], [TermsId], [RolloverProductId], [CommissionId], [CommissionAmt], [CancelFeeId], [MonthlyChargeId], [ProductCode], [RatePackageId], [ProductName], [TermDate], [DiscountTypeId], [DiscountAmount], [ProductZoneID], [IsGreen], [IsBestChoice], [ActiveEnrollmentFlag], [DepositAmount], [CreditScoreThreshold], [Incentives]
FROM  saes_Texpo..Product WHERE ProductId IN (SELECT ProductID FROM saes_Texpo..Contract WHERE CustID = @CustID)
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Product WHERE ProductId IN (SELECT ProductID FROM saes_Texpo..Contract WHERE CustID = @CustID))
SET IDENTITY_INSERT daes_Texpo..Product OFF

PRINT 'Copy Contract'
SET IDENTITY_INSERT daes_Texpo..Contract ON

INSERT INTO daes_Texpo..Contract ([ContractID], [ContractName], [ContractLength], [SignedDate], [BeginDate], [EndDate], [TermDate], [TermLength], [AnnualUsage], [CurePeriod], [Terms], [ContractTypeID], [ContactName], [ContactPhone], [ContactFax], [AccountManagerID], [Bandwidth], [FinanceCharge], [ProductID], [MeterChargeCode], [AggregatorFee], [CreatedByID], [CreateDate], [ActiveFlag], [TDSPTemplateID], [RateCode], [RateID], [CustID], [AutoRenewFlag], [RenewalRate], [RenewalStartDate], [RenewalTerm], [ContractNumber], [ChangeReason], [ContractTerm])
SELECT  [ContractID], [ContractName], [ContractLength], [SignedDate], [BeginDate], [EndDate], [TermDate], [TermLength], [AnnualUsage], [CurePeriod], [Terms], [ContractTypeID], [ContactName], [ContactPhone], [ContactFax], [AccountManagerID], [Bandwidth], [FinanceCharge], [ProductID], [MeterChargeCode], [AggregatorFee], [CreatedByID], [CreateDate], [ActiveFlag], [TDSPTemplateID], [RateCode], [RateID], [CustID], [AutoRenewFlag], [RenewalRate], [RenewalStartDate], [RenewalTerm], [ContractNumber], [ChangeReason], [ContractTerm]
FROM  saes_Texpo..Contract WHERE CustID = @CustID
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..Contract WHERE CustID = @CustID)
SET IDENTITY_INSERT daes_Texpo..Contract OFF

PRINT 'Copy AccountsReceivable'
SET IDENTITY_INSERT daes_Texpo..AccountsReceivable ON

INSERT INTO daes_Texpo..AccountsReceivable ( [AcctsRecID], [ResetDate], [ARDate], [PrevBal], [CurrInvs], [CurrPmts], [CurrAdjs], [BalDue], [LateFee], [LateFeeRate], [LateFeeMaxAmount], [LateFeeTypeID], [AuthorizedPymt], [PastDue], [BalAge0], [BalAge1], [BalAge2], [BalAge3], [BalAge4], [BalAge5], [BalAge6], [Deposit], [DepositBeginDate], [PaymentPlanFlag], [PaymentPlanTrueUpFlag], [PaymentPlanAmount], [PaymentPlanTrueUpPeriod], [PaymentPlanTrueUpThresholdAmount], [PaymentPlanTrueUpType], [PaymentPlanEffectiveDate], [PrePaymentFlag], [PrePaymentDailyAmount], [CapitalCredit], [Terms], [StatusID], [GracePeriod], [Migr_acct_no], [Migr_div_code], [Migr_service_type], [PaymentPlanTotalVariance], [PaymentPlanVarianceUnit], [InvoiceMinimumAmount], [LateFeeThresholdAmt], [LastInvoiceAcctsRecHistID], [LastPaymentAcctsRecHistID], [LastAdjustmentAcctsRecHistID], [CancelFeeTypeId], [CancelFeeAmount], [DeferredBalance])
SELECT   [AcctsRecID], [ResetDate], [ARDate], [PrevBal], [CurrInvs], [CurrPmts], [CurrAdjs], [BalDue], [LateFee], [LateFeeRate], [LateFeeMaxAmount], [LateFeeTypeID], [AuthorizedPymt], [PastDue], [BalAge0], [BalAge1], [BalAge2], [BalAge3], [BalAge4], [BalAge5], [BalAge6], [Deposit], [DepositBeginDate], [PaymentPlanFlag], [PaymentPlanTrueUpFlag], [PaymentPlanAmount], [PaymentPlanTrueUpPeriod], [PaymentPlanTrueUpThresholdAmount], [PaymentPlanTrueUpType], [PaymentPlanEffectiveDate], [PrePaymentFlag], [PrePaymentDailyAmount], [CapitalCredit], [Terms], [StatusID], [GracePeriod], [Migr_acct_no], [Migr_div_code], [Migr_service_type], [PaymentPlanTotalVariance], [PaymentPlanVarianceUnit], [InvoiceMinimumAmount], [LateFeeThresholdAmt], [LastInvoiceAcctsRecHistID], [LastPaymentAcctsRecHistID], [LastAdjustmentAcctsRecHistID], [CancelFeeTypeId], [CancelFeeAmount], [DeferredBalance]
FROM  saes_Texpo..AccountsReceivable WHERE AcctsRecID IN (SELECT AcctsRecID FROM saes_Texpo..Customer WHERE CustID = @CustID)
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..AccountsReceivable WHERE AcctsRecID IN (SELECT AcctsRecID FROM saes_Texpo..Customer WHERE CustID = @CustID))
SET IDENTITY_INSERT daes_Texpo..AccountsReceivable OFF

PRINT 'Copy CustomerAdditionalInfo'
--SET IDENTITY_INSERT daes_Texpo..CustomerAdditionalInfo ON

INSERT INTO daes_Texpo..CustomerAdditionalInfo ([CustID], [CSPDUNSID], [BillingTypeID], [BillingDayOfMonth], [MasterCustID_2], [MasterCustID_3], [MasterCustID_4], [TaxAssessment], [ContractPeriod], [ContractDate], [AccessVerificationType], [AccessVerificationData], [ClientAccountNo], [InstitutionID], [TransitNum], [AccountNum], [MigrationAccountNo], [MigrationFirstServed], [MigrationKwh], [CollectionsStageID], [CollectionsStatus], [CollectionsDate], [KeyAccount], [DisconnectLtr], [AuthorizedReleaseName], [AuthorizedReleaseDOB], [AuthorizedReleaseFederalTaxID], [EFTFlag], [PromiseToPayFlag], [DisconnectFlag], [CreditHoldFlag], [RawConsumptionImportFlag], [CustomerProtectionStatus], [MCPEFlag], [HasLocationMasterFlag], [DivisionID], [DivisionCode], [DriverLicenseNo], [PromotionCodeID], [CustomerDUNS], [CustomerGroupID], [DeceasedFlag], [BankruptFlag], [CollectionsAgencyID], [DoNotCall], [CustomerSecretWord], [PrintGroupID], [IsDPP], [SubsequentDepositExempt], [AutoPayFlag], [SpecialNeedsFlag], [SpecialNeedsEndDate], [SpecialNeedsQualifierTypeID], [CurrentCustNo], [CsrImportDate], [EarlyTermFee], [EarlyTermFeeUpdateDate], [OnSwitchHold], [SwitchHoldStartDate], [DPPStatusID], [UpdateContactInfoFlag], [IsFriendlyLatePaymentReminderSent], [IsLowIncome], [ExtendedCustTypeId], [AutoPayLastUpdated], [UnitNumber], [CustomerCategoryID], [GreenEnergyOptIn], [SocialCauseID], [SocialCauseCode], [SecondaryContactFirstName], [SecondaryContactLastName], [SecondaryContactPhone], [SecondaryContactRelationId], [SSN], [ServiceAccount], [EncryptionPasswordTypeId], [EncryptionPasswordCustomValue], [CustInfo1], [CustInfo2], [CustInfo3], [CustInfo4], [CustInfo5], [SalesAgent], [Broker], [PromoCode], [CommissionType], [CommissionAmount], [ReferralID], [CampaignName], [AccessDBID], [SalesChannel], [IsPUCComplaint], [TCPAAuthorization], [CancellationFee], [MunicipalAggregation])
SELECT  [CustID], [CSPDUNSID], [BillingTypeID], [BillingDayOfMonth], [MasterCustID_2], [MasterCustID_3], [MasterCustID_4], [TaxAssessment], [ContractPeriod], [ContractDate], [AccessVerificationType], [AccessVerificationData], [ClientAccountNo], [InstitutionID], [TransitNum], [AccountNum], [MigrationAccountNo], [MigrationFirstServed], [MigrationKwh], [CollectionsStageID], [CollectionsStatus], [CollectionsDate], [KeyAccount], [DisconnectLtr], [AuthorizedReleaseName], [AuthorizedReleaseDOB], [AuthorizedReleaseFederalTaxID], [EFTFlag], [PromiseToPayFlag], [DisconnectFlag], [CreditHoldFlag], [RawConsumptionImportFlag], [CustomerProtectionStatus], [MCPEFlag], [HasLocationMasterFlag], [DivisionID], [DivisionCode], [DriverLicenseNo], [PromotionCodeID], [CustomerDUNS], [CustomerGroupID], [DeceasedFlag], [BankruptFlag], [CollectionsAgencyID], [DoNotCall], [CustomerSecretWord], [PrintGroupID], [IsDPP], [SubsequentDepositExempt], [AutoPayFlag], [SpecialNeedsFlag], [SpecialNeedsEndDate], [SpecialNeedsQualifierTypeID], [CurrentCustNo], [CsrImportDate], [EarlyTermFee], [EarlyTermFeeUpdateDate], [OnSwitchHold], [SwitchHoldStartDate], [DPPStatusID], [UpdateContactInfoFlag], [IsFriendlyLatePaymentReminderSent], [IsLowIncome], [ExtendedCustTypeId], [AutoPayLastUpdated], [UnitNumber], [CustomerCategoryID], [GreenEnergyOptIn], [SocialCauseID], [SocialCauseCode], [SecondaryContactFirstName], [SecondaryContactLastName], [SecondaryContactPhone], [SecondaryContactRelationId], [SSN], [ServiceAccount], [EncryptionPasswordTypeId], [EncryptionPasswordCustomValue], [CustInfo1], [CustInfo2], [CustInfo3], [CustInfo4], [CustInfo5], [SalesAgent], [Broker], [PromoCode], [CommissionType], [CommissionAmount], [ReferralID], [CampaignName], [AccessDBID], [SalesChannel], [IsPUCComplaint], [TCPAAuthorization], [CancellationFee], [MunicipalAggregation]
FROM  saes_Texpo..CustomerAdditionalInfo WHERE CustID = @CustID
AND NOT EXISTS(SELECT 1 FROM daes_Texpo..CustomerAdditionalInfo WHERE CustID = @CustID)
--SET IDENTITY_INSERT daes_Texpo..CustomerAdditionalInfo OFF

PRINT 'END Copy Customer'
";
            DB.ExecuteQuery(sql, _appConfig.ConnectionCsr);
        }

        private static void MakeCustomerEligibleForDisconnectLetter(string custNo)
        {
            string sql = $@"USE daes_Texpo
DECLARE @CustNo AS VARCHAR(20) = '{custNo}'
DECLARE @CustID AS INT = (SELECT CustID FROM saes_Texpo..Customer WHERE CustNo = {custNo})
UPDATE AccountsReceivable SET BalAge1=25.45 WHERE AcctsRecID = (SELECT AcctsRecID FROM Customer WHERE CustId = @CustID)
DELETE FROM Letter WHERE CustId = @CustID
UPDATE Premise SET StatusID = 10, EndServiceDate = NULL WHERE CustId = @CustID
DELETE FROM CustomerDisconnect WHERE CustId = @CustID
DELETE FROM MethodLog WHERE MethodId IN (310, 311, 314, 339, 341, 340, 313, 348, 9000, 9010, 6000, 6001)";

            DB.ExecuteQuery(sql, _appConfig.ConnectionCsr);
        }

        private static void GenerateEvents(List<int> eventTypeIds)
        {
            var list = CIS.Element.Core.Event.EventTypeList.Load(_clientConfig.ClientId);

            eventTypeIds.ForEach(id =>
            {
                var htParams = new Hashtable { { "EventTypeID", id } };
                var _event = list.SingleOrDefault(x => x.EventTypeID == id);
                new CIS.Engine.Event.EventGenerator().GenerateEvent(_clientConfig.ClientId, htParams, _clientConfig.Client, _appConfig.ConnectionCsr, _clientConfig.ConnectionBillingAdmin, _event.AssemblyName, _event.ClassName);
            });
        }

        private static void GenerateEvents()
        {
            var maintenance = new MyMaintenance(_appConfig.ConnectionCsr, _appConfig.ConnectionMarket, _clientConfig.ConnectionBillingAdmin);
            maintenance.GenerateEvents();
        }

        private static void ExecuteExport()
        {
            //exec csp_GetServiceMethods 2 (Export = ExportTransactions, EncryptFiles, TransmitFiles, ExportLoadForecasting, ProcessEFTPayments, RunAutoDNPProcess, LogService)
            var myExport = new MyExport(_appConfig.ConnectionMarket, _appConfig.ConnectionCsr, _clientConfig.ConnectionBillingAdmin);
            myExport.MyExportTransactions();
            //myExport.EncryptFiles();
            //myExport.MyTransmitFiles();
            //myExport.MyExportLoadForecasting();
            //myExport.MyProcessEFTPayments();
            //myExport.MyRunAutoDNPProcess();
            //myExport.MyLogService();

            /*
            var exporter = new CIS.Export.Billing.Market814(_clientConfiguration.ConnectionMarket, _clientConfiguration.ConnectionCsr)
            {
                HistoricalUsageRequestType = _clientConfiguration.ExportHistoricalUsageRequestType,
                ConnectionAdmin = Utility.BillingAdminDEV,
                Client = _clientConfiguration.ConnectionCsr,
                ClientID = Utility.Clients["TXP"]
            };
            exporter.Export();
            */
        }

        private static void GenerateSimpleMarketTransactionEvaluationEvents()
        {
            var hashTable = new Hashtable();
            var gen1 = new CIS.Framework.Event.EventGenerator.CustomerEvaluation(_appConfig.ConnectionCsr, _clientConfig.ConnectionBillingAdmin);
            if (gen1.Generate(_clientConfig.ClientId, hashTable))
            {
                Console.WriteLine("CustomerEvaluation Events Generated");
            }
            else
            {
                Console.WriteLine("CustomerEvaluation Events could not generated");
            }
            var gen = new CIS.Framework.Event.EventGenerator.SimpleMarketTransactionEvaluation(_appConfig.ConnectionCsr, _clientConfig.ConnectionBillingAdmin);


            if (gen.Generate(_clientConfig.ClientId, hashTable))
            {
                Console.WriteLine("SimpleMarketTransactionEvaluation Events Generated");
            }
            else
            {
                Console.WriteLine("SimpleMarketTransactionEvaluation Events could not generated");
            }
        }

        private static void ProcessEvents()
        {
            var engine = new CIS.Engine.Event.Queue(_clientConfig.ConnectionBillingAdmin);
            engine.ProcessEventQueue(_appConfig.ClientID, _appConfig.ConnectionCsr, _appConfig.ConnectionMarket, _appConfig.ClientAbbreviation);
        }
        
    }
}