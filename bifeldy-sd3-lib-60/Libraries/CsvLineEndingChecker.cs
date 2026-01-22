namespace bifeldy_sd3_lib_60.Libraries {

    public enum LineEndingType {
        LF,
        CRLF,
        Mixed,
        Unknown
    }

    public static class CsvLineEndingChecker {

        public static LineEndingType DetectLineEndings(string filePath, int bufferSize = 1024) {
            int crCount = 0;
            int lfCount = 0;
            int crlfCount = 0;
            bool wasCrLast = false;

            try {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                    long bytesToRead = Math.Min(bufferSize, fs.Length);

                    byte[] buffer = new byte[bytesToRead];
                    _ = fs.Read(buffer, 0, (int)bytesToRead);

                    for (int i = 0; i < buffer.Length; i++) {
                        char currentChar = (char)buffer[i];

                        if (currentChar == '\r') {
                            crCount++;
                            wasCrLast = true;
                        }
                        else if (currentChar == '\n') {
                            lfCount++;
                            if (wasCrLast) {
                                crlfCount++;
                            }
                            wasCrLast = false;
                        }
                        else {
                            wasCrLast = false;
                        }
                    }
                }

                if (lfCount == 0) {
                    return LineEndingType.Unknown;
                }
                else if (crCount == crlfCount && crCount > 0) {
                    return LineEndingType.CRLF;
                }
                else if (crCount == 0) {
                    return LineEndingType.LF;
                }
                else {
                    return LineEndingType.Mixed;
                }
            }
            catch (IOException e) {
                return LineEndingType.Unknown;
            }
        }

    }

}
