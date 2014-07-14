﻿//#define TEST_TOOL
#define TESTNET3
//#define MEMORY
//#define DUMMY_MONITOR

using BitSharp.Common.ExtensionMethods;
using BitSharp.Node;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Ninject;
using Ninject.Modules;
using NLog;
using BitSharp.Core.Rules;
using BitSharp.Core.Storage;
using BitSharp.Core;
using BitSharp.Esent;
using BitSharp.Node.Storage;
using BitSharp.Wallet;
using BitSharp.Wallet.Address;

namespace BitSharp.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IKernel kernel;
        private Logger logger;
        private CoreDaemon coreDaemon;
        private LocalClient localClient;
        private MainWindowViewModel viewModel;

        public MainWindow()
        {
            try
            {
                //TODO
                //MainnetRules.BypassValidation = true;
                MainnetRules.IgnoreScripts = true;
                MainnetRules.IgnoreSignatures = false;
                MainnetRules.IgnoreScriptErrors = true;

#if TEST_TOOL
                var rulesType = RulesEnum.ComparisonToolTestNet;
#elif TESTNET3
                var rulesType = RulesEnum.TestNet3;
#else
                var rulesType = RulesEnum.MainNet;
#endif

                // initialize kernel
                this.kernel = new StandardKernel();

                // add logging module
                this.kernel.Load(new LoggingModule(LogLevel.Info));

                // log startup
                this.logger = kernel.Get<Logger>();
                this.logger.Info("Starting up: {0}".Format2(DateTime.Now));

                var modules = new List<INinjectModule>();

                // add storage module
#if TEST_TOOL
                modules.Add(new MemoryStorageModule());
#elif MEMORY
                modules.Add(new MemoryStorageModule());
#else
                string baseDirectory;
                if (Debugger.IsAttached)
                    baseDirectory = Path.Combine(Config.LocalStoragePath, "Debugger", "Data");
                else
                    baseDirectory = Path.Combine(Config.LocalStoragePath, "Data");
                
                modules.Add(new EsentStorageModule(baseDirectory, rulesType, cacheSizeMaxBytes: int.MaxValue - 1));
                //ChainStateBuilderStorage.IndexOutputs = true;
#endif

                // add cache modules
                modules.Add(new NodeCacheModule());

                // add rules module
                modules.Add(new RulesModule(rulesType));

                // load modules
                this.kernel.Load(modules.ToArray());

                // initialize the blockchain daemon
                this.coreDaemon = this.kernel.Get<CoreDaemon>();
                this.kernel.Bind<CoreDaemon>().ToConstant(this.coreDaemon).InTransientScope();

#if DUMMY_MONITOR
                var dummyMonitor = new DummyMonitor(this.logger);
                blockchainDaemon.SubscribeChainStateVisitor(dummyMonitor);
#endif

                // initialize p2p client
                this.localClient = this.kernel.Get<LocalClient>();
                this.kernel.Bind<LocalClient>().ToConstant(this.localClient).InTransientScope();

                // setup view model
#if DUMMY_MONITOR
                this.viewModel = new MainWindowViewModel(this.kernel, dummyMonitor);
#else
                this.viewModel = new MainWindowViewModel(this.kernel);
#endif
                InitializeComponent();
                this.viewModel.ViewBlockchainLast();

                // start the blockchain daemon
                this.coreDaemon.Start();

                // start p2p client
                var startThread = new Thread(() => this.localClient.Start());
                startThread.Name = "LocalClient.Start";
                startThread.Start();

                this.DataContext = this.viewModel;

#if TEST_TOOL
                var bitcoinjThread = new Thread(
                    ()=>
                    {
                        var projectFolder = Environment.CurrentDirectory;
                        while (projectFolder.Contains(@"\BitSharp.Client"))
                            projectFolder = Path.GetDirectoryName(projectFolder);

                        File.Delete(Path.Combine(projectFolder, "Bitcoinj-comparison.log"));

                        var javaProcessStartInfo = new ProcessStartInfo
                            {
                                FileName = @"C:\Program Files\Java\jdk1.7.0_25\bin\java.exe",
                                WorkingDirectory = projectFolder,
                                Arguments = @"-Djava.util.logging.config.file={0}\bitcoinj.log.properties -jar {0}\bitcoinj.jar".Format2(projectFolder),
                                UseShellExecute = false
                            };

                        var javaProcess = Process.Start(javaProcessStartInfo);
                    });
                bitcoinjThread.Start();
#endif
            }
            catch (Exception e)
            {
                if (this.logger != null)
                {
                    this.logger.FatalException("Application failed", e);
                    LogManager.Flush();
                }
                else
                {
                    Console.WriteLine(e);
                }

                Environment.Exit(-1);
            }
        }

        public MainWindowViewModel ViewModel { get { return this.viewModel; } }

        protected override void OnClosed(EventArgs e)
        {
            // shutdown
            new IDisposable[]
            {
                this.localClient,
                this.coreDaemon,
                this.kernel
            }.DisposeList();

            base.OnClosed(e);
        }

        private void ViewFirst_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.ViewBlockchainFirst();
        }

        private void ViewPrevious_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.ViewBlockchainPrevious();
        }

        private void ViewNext_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.ViewBlockchainNext();
        }

        private void ViewLast_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.ViewBlockchainLast();
        }

#if DUMMY_MONITOR
        private sealed class DummyMonitor : WalletMonitor
        {
            public DummyMonitor(Logger logger)
                : base(logger)
            {
                this.AddAddress(new First10000Address());
                this.AddAddress(new Top10000Address());
            }
        }
#endif
    }
}
