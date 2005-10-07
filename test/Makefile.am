CSC = mcs -debug
RUNTIME = mono

CSFLAGS =

ALL_CSFILES = 		\
	test-01.cs	\
	test-02.cs	\
	test-03.cs	\
	test-04.cs	\
	test-05.cs	\
	test-06.cs

ALL_EXE = $(ALL_CSFILES:%.cs=%.exe)

ALL_OUTFILES = $(ALL_CSFILES:%.cs=%.outfile)

%.exe: $(srcdir)/%.cs
	$(CSC) -out:$@ $(CSFLAGS) $^

%.outfile: %.exe
	@echo "generating $@"
	LD_LIBRARY_PATH=../profiler/.libs:$LD_LIBRARY_PATH $(RUNTIME) --profile=heap-buddy:$@ $^

test: $(ALL_OUTFILES)

EXTRA_DIST =		\
	$(ALL_CSFILES)

CLEANFILES =			\
	$(ALL_EXE)		\
	$(ALL_EXE:%=%.mdb)	\
	$(ALL_OUTFILES)
