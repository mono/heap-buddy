
CSC = mcs -debug
CSFLAGS = -target:exe -r:Mono.Cairo -pkg:gtk-sharp-2.0 -r:System.Drawing.dll 

TARGET = HeapBuddy.exe
WRAPPER = heap-buddy

REPORT_CSFILES =		\
	BacktracesReport.cs	\
	HistoryReport.cs	\
	SummaryReport.cs	\
	TypesReport.cs		\
	MemlogReport.cs		\
	GraphReport.cs

CSFILES = 			\
	HeapBuddy.cs		\
	Util.cs			\
	Table.cs		\
	ObjectStats.cs		\
	Type.cs			\
	Frame.cs		\
	Backtrace.cs		\
	Gc.cs			\
	Resize.cs		\
	OutfileReader.cs	\
	Report.cs		\
	MemZone.cs		\
	TypeLog.cs		\
	MethodLog.cs		\
	Graphics.cs		\
	$(REPORT_CSFILES)

bin_SCRIPTS =			\
	$(WRAPPER)

SRCDIR_CSFILES = $(CSFILES:%=$(srcdir)/%)

$(TARGET): $(SRCDIR_CSFILES)
	$(CSC) -out:$@ $(CSFLAGS) $^

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|g" < $(srcdir)/$(WRAPPER).in > $@
	chmod +x $(WRAPPER)

all: $(TARGET) $(WRAPPER)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(TARGET)


EXTRA_DIST = 			\
	$(CSFILES)		\
	$(WRAPPER).in

CLEANFILES =			\
	$(TARGET)		\
	$(TARGET).mdb		\
	$(WRAPPER)
