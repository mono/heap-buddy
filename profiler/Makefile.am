
lib_LTLIBRARIES = libmono-profiler-heap-buddy.la

libmono_profiler_heap_buddy_la_SOURCES = 	\
	accountant.h				\
	accountant.c				\
	backtrace.h				\
	backtrace.c				\
	outfile-writer.h			\
	outfile-writer.c			\
	heap-buddy.c

libmono_profiler_heap_buddy_la_LIBADD = @PROFILER_LIBS@

INCLUDES = @PROFILER_CFLAGS@ -Wall