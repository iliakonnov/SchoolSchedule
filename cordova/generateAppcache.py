import os
import glob
import hashlib
import base64

blacklist = [i.lower() for i in ['manifest.appcache', 'scheduleData', 'js/generated.js']]
paths = ['./platforms/browser/platform_www/', './www/']
outPaths = ['./www/manifest.appcache', './platforms/browser/www/manifest.appcache']
outJsPaths = [
    './www/js/generated.cached.js', './platforms/browser/www/js/generated.cached.js',
    './www/js/generated.js', './platforms/browser/www/js/generated.js'
]

size = 0
md5 = hashlib.md5()
sha1 = hashlib.sha1()
blake = hashlib.blake2s()
tr = str.maketrans({
    ";": "w",
    "<": "x",
    ">": "v",
    "\\": "{",
    "`": "z",
    '"': "y",
    "'": "}"
})
resultJs = 'CACHED_FILES = {'
result = '''CACHE MANIFEST

CACHE:'''
for p in paths:
    for filename in (j.replace('\\', '/') for j in glob.iglob(p + '**', recursive=True)):
        normalized = filename.replace(p, '')
        if os.path.isfile(filename) and normalized.lower() not in blacklist:
            print(' OK : ' + filename + ' => ' + normalized)
            fileHash = hashlib.md5()
            with open(filename, 'rb') as f:
                chunk = f.read(4096)
                while chunk:
                    size += len(chunk)
                    md5.update(chunk)
                    sha1.update(chunk)
                    blake.update(chunk)
                    fileHash.update(chunk)
                    chunk = f.read(4096)
            fHash = base64.a85encode(fileHash.digest()).decode('utf8').translate(tr)
            result += '\n\n/' + normalized
            result += '\n# ' + fHash
            resultJs += '\n    "{name}": "{hash}",'.format(
                name=normalized,
                hash=fHash
            )
        else:
            print('SKIP: ' + filename + ' => ' + normalized)

encodedHashes = {
    'md5': base64.a85encode(md5.digest()).decode('utf8').translate(tr),
    'sha1': base64.a85encode(sha1.digest()).decode('utf8').translate(tr),
    'blake': base64.a85encode(blake.digest()).decode('utf8').translate(tr),
    'size': size
}

result += '''

# TOTAL:
# md5: {md5}
# sha1: {sha1}
# blake: {blake}
# size: {size}

NETWORK:
*
'''.format(**encodedHashes)

for i in outPaths:
    with open(i, 'w') as f:
        f.write(result)

resultJs += '''
}};
TOTAL_MD5 = '{md5}';
TOTAL_SHA1 = '{sha1}';
TOTAL_BLAKE = '{blake}';
TOTAL_SIZE = {size};
'''.format(**encodedHashes)
for i in outJsPaths:
    with open(i, 'w') as f:
        f.write(resultJs)
