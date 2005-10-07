
CSC = mcs -debug
CSFLAGS = -target:exe

TARGET = HeapBuddy.exe
WRAPPER = heap-buddy

REPORT_CSFILES =		\
	HistoryReport.cs	\
	TypesReport.cs

CSFILES = 			\
	HeapBuddy.cs		\
	Util.cs			\
	Table.cs		\
	ObjectStats.cs		\
	Type.cs			\
	Gc.cs			\
	Resize.cs		\
	OutfileReader.cs	\
	Report.cs		\
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
	$(SRCDIR_CSFILES)	\
	$(WRAPPER).in

CLEANFILES =			\
	$(TARGET)		\
	$(TARGET).mdb		\
	$(WRAPPER)