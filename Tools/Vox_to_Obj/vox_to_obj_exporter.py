"""
    This script is designed to export a mass amount of MagicaVoxel .vox files
    to .obj. Unlike Magica's internal exporter, this exporter preserves the
    voxel vertices for easy manipulating in a 3d modeling program like Blender.
    
    Various meshing algorithms are included (or to be included). MagicaVoxel
    uses monotone triangulation (I think). The algorithms that will (or do)
    appear in this script will use methods to potentially reduce rendering
    artifacts that could be introduced by triangulation of this nature.
    
    I may also include some features like light map generation for easy
    importing into Unreal Engine, etc.
    
    Notes:
        * There may be a few floating point equality comparisons. They seem to
            work but it scares me a little.
        * TODO: use constants instead of magic numbers (as defined in AAQuad),
                (i.e., ..., 2 -> AAQuad.TOP, ...)
        * A lot of assertions should probably be exceptions since they are
            error checking user input (this sounds really bad now that I've put
            it on paper...). So don't run in optimized mode (who does that
            anyways?).
        * I am considering adding FBX support.
"""
import math

class AAQuad:
    """ A solid colored axis aligned quad. """
    normals = [
        (-1, 0, 0), # left = 0
        (1, 0, 0),  # right = 1
        (0, 0, 1),  # top = 2
        (0, 0, -1), # bottom = 3
        (0, -1, 0), # front = 4
        (0, 1, 0)   # back = 5
    ]
    LEFT = 0
    RIGHT = 1
    TOP = 2
    BOTTOM = 3
    FRONT = 4
    BACK = 5
    def __init__(self, verts, uv=None, normal=None):
        assert len(verts) == 4, "face must be a quad"
        self.vertices = verts
        self.uv = uv
        self.normal = normal
    def __str__(self):
        s = []
        for i in self.vertices:
            s.append( str(i) + '/' + str(self.uv) + '/' + str(self.normal))
        return 'f ' + ' '.join(s)
    def center(self):
        return (
            sum(i[0] for i in self.vertices)/4,
            sum(i[1] for i in self.vertices)/4,
            sum(i[2] for i in self.vertices)/4
        )

def bucketHash(faces, origin, maximum, bucket=16):
    extents = (
        math.ceil((maximum[0] - origin[0])/bucket),
        math.ceil((maximum[1] - origin[1])/bucket),
        math.ceil((maximum[2] - origin[2])/bucket)
    )
    buckets = {}
    for f in faces:
        c = f.center()
        # TODO

def optimizedGreedyMesh(faces):
    # TODO
    edges = adjacencyGraphEdges(faces)
    groups = contiguousFaces(faces, edges)
    return faces

def adjacencyGraphEdges(faces):
    """ Get the list of edges representing adjacent faces. """
    # a list of edges, where edges are tuple(face_a, face_b)
    edges = []
    # build the list of edges in the graph
    for root in faces:
        for face in faces:
            if face is root:
                continue
            if facesAreAdjacent(root, face):
                # the other edge will happen somewhere else in the iteration
                # (i.e., the relation isAdjacent is symmetric)
                edges.append((root, face))
    return edges

def contiguousFaces(faces, adjacencyGraphEdges):
    """ Get the list of connected components from a list of graph edges.
        The list will contain lists containing the edges within the components.
    """
    groups = []
    visited = dict((f, False) for f in faces)
    for face in faces:
        # if the face hasn't been visited, it is not in any found components
        if not visited[face]:
            g = []
            _visitGraphNodes(face, adjacencyGraphEdges, visited, g)
            # there is only a new component if face has not been visited yet
            groups.append(g)
    return groups

def _visitGraphNodes(node, edges, visited, component):
    """ Recursive routine used in findGraphComponents """
    # visit every component connected to this one
    for edge in edges:
        # for all x in nodes, (node, x) and (x, node) should be in edges!
        # therefore we don't have to check for "edge[1] is node"
        if edge[0] is node and not visited[edge[1]]:
            assert edge[1] is not node, "(node, node) should not be in edges"
            # mark the other node as visited
            visited[edge[1]] = True
            component.append(edge[1])
            # visit all of that nodes connected nodes
            _visitGraphNodes(edge[1], edges, visited, component)

def facesAreAdjacent(a, b):
    """ Adjacent is defined as same normal, uv, and a shared edge.
        This isn't entirely intuitive (i.e., corner faces are not adjacent)
        but this definition fits the problem domain.
        Only works on AAQuads.
    """
    # note: None is == None, this shouldn't matter
    if a.uv != b.uv:
        return False
    if a.normal != b.normal:
        return False
    # to be adjacent, two faces must share an edge
    # use == and not identity in case edge split was used
    shared = 0
    for vert_a in a.vertices:
        for vert_b in b.vertices:
            if vert_a == vert_b:
                shared += 1
            # hooray we have found a shared edge (or a degenerate case...)
            if shared == 2:
                return True
    return False

class GeoFace:
    """ An arbitrary geometry face
        This should only be used for arbitrary models, not ones we can
        reasonably assume are axis aligned.
    """
    def __init__(self, verts, uvs=None, normals=None):
        self.vertices = verts
        assert len(verts) in (3, 4), "only quads and tris are supported"
        self.normals = normals
        self.uvs = uvs
    def toAAQuad(self, skipAssert=False):
        q = AAQuad(self.vertices)
        if self.normals is not None and len(self.normals) > 0:
            if not skipAssert:
                for i in self.normals:
                    assert self.normals[0] == i, \
                        "face must be axis aligned (orthogonal normals)"
            q.normal = self.normals[0]
        if self.uvs is not None and len(self.uvs) > 0:
            if not skipAssert:
                for i in self.uvs:
                    assert self.uvs[0] == i, \
                        "face must be axis aligned (orthogonal)"
            q.uv = self.uvs[0]
        return q

class VoxelStruct:
    """ Describes a voxel object
    """
    def __init__(self):
        # a dict is probably the best way to go about this
        # (as a trade off between performance and code complexity)
        # see _index for the indexing method
        self.voxels = {}
        self.colorIndices = set()
    def fromList(self, voxels):
        self.voxels = {}
        for voxel in voxels:
            self.setVoxel(voxel)
            self.colorIndices.add(voxel.colorIndex)
    def setVoxel(self, voxel):
        self.voxels[voxel.z*(256**2) + voxel.y * 256 + voxel.x] = voxel
    def getVoxel(self, x, y, z):
        return self.voxels.get(z*(256**2) + y * 256 + x, None)
    def _index(self, x, y, z):
        return z*(256**2) + y * 256 + x
    def getBounds(self):
        origin = (float("inf"), float("inf"), float("inf"))
        maximum = (float("-inf"), float("-inf"), float("-inf"))
        for key, voxel in self.voxels.items():
            origin = (
                min(origin[0], voxel.x),
                min(origin[1], voxel.y),
                min(origin[2], voxel.z)
            )
            maximum = (
                max(maximum[0], voxel.x),
                max(maximum[1], voxel.y),
                max(maximum[2], voxel.z)
            )
        return origin, maximum
    def zeroOrigin(self):
        """ Translate the model so that it's origin is at 0, 0, 0 """
        origin, maximum = self.getBounds()
        result = {}
        xOff, yOff, zOff = origin
        for key, voxel in self.voxels.iteritems():
            result[self._index(voxel.x-xOff, voxel.y-yOff, voxel.z-zOff)] = \
                Voxel(voxel.x-xOff, voxel.y-yOff, voxel.z-zOff,
                      voxel.colorIndex)
        self.voxels = result
        return (0, 0, 0), (maximum[0] - xOff,
                           maximum[1] - yOff,
                           maximum[2] - zOff)
    def toQuads(self):
        """ --> a list of AAQuads """
        faces = []
        for key, voxel in self.voxels.items():
            self._getObjFaces(voxel, faces)
        return faces
    def _getObjFaces(self, voxel, outFaces):
        if voxel.colorIndex == 0:
            # do nothing if this is an empty voxel
            # n.b., I do not know if this ever can happen.
            return []
        sides = self._objExposed(voxel)
        if sides[0]:
            f = self._getLeftSide(voxel)
            self._getObjFacesSupport(0, voxel.colorIndex, f, outFaces)
        if sides[1]:
            f = self._getRightSide(voxel)
            self._getObjFacesSupport(1, voxel.colorIndex, f, outFaces)
        if sides[2]:
            f = self._getTopSide(voxel)
            self._getObjFacesSupport(2, voxel.colorIndex, f, outFaces)
        if sides[3]:
            f = self._getBottomSide(voxel)
            self._getObjFacesSupport(3, voxel.colorIndex, f, outFaces)
        if sides[4]:
            f = self._getFrontSide(voxel)
            self._getObjFacesSupport(4, voxel.colorIndex, f, outFaces)
        if sides[5]:
            f = self._getBackSide(voxel)
            self._getObjFacesSupport(5, voxel.colorIndex, f, outFaces)
        return
        n = AAQuad.normals[i]
        # note: texcoords are based on MagicaVoxel's texturing scheme!
        #   meaning a color index of 0 translates to pixel[255]
        #   and color index [1:256] -> pixel[0:255]
        u = ((voxel.colorIndex - 1)/256 + 1/512, 0.5)
        outFaces.append(
            # this is most definitely not "fun"
            AAQuad(f, u, n)
        )
    def _getObjFacesSupport(self, side, color, faces, outFaces):
        n = AAQuad.normals[side]
        # note: texcoords are based on MagicaVoxel's texturing scheme!
        #   meaning a color index of 0 translates to pixel[255]
        #   and color index [1:256] -> pixel[0:255]
        u = ((color - 1)/256 + 1/512, 0.5)
        outFaces.append(
            # fact: the parameters were coincidentally "f, u, n" at one point!
            AAQuad(faces, u, n)
        )
    # MagicaVoxel does -.5 to +.5 for each cube, we'll do 0.0 to 1.0 ;)
    def _getLeftSide(self, voxel):
        return [
            (voxel.x, voxel.y + 1, voxel.z + 1),
            (voxel.x, voxel.y + 1, voxel.z),
            (voxel.x, voxel.y, voxel.z),
            (voxel.x, voxel.y, voxel.z + 1)
        ]
    def _getRightSide(self, voxel):
        return (
            (voxel.x + 1, voxel.y, voxel.z + 1),
            (voxel.x + 1, voxel.y, voxel.z),
            (voxel.x + 1, voxel.y + 1, voxel.z),
            (voxel.x + 1, voxel.y + 1, voxel.z + 1)
        )
    def _getTopSide(self, voxel):
        return (
            (voxel.x, voxel.y + 1, voxel.z + 1),
            (voxel.x, voxel.y, voxel.z + 1),
            (voxel.x + 1, voxel.y, voxel.z + 1),
            (voxel.x + 1, voxel.y + 1, voxel.z + 1)
        )
    def _getBottomSide(self, voxel):
        return (
            (voxel.x, voxel.y, voxel.z),
            (voxel.x, voxel.y + 1, voxel.z),
            (voxel.x + 1, voxel.y + 1, voxel.z),
            (voxel.x + 1, voxel.y, voxel.z)
        )
    def _getFrontSide(self, voxel):
        return (
            (voxel.x, voxel.y, voxel.z + 1),
            (voxel.x, voxel.y, voxel.z),
            (voxel.x + 1, voxel.y, voxel.z),
            (voxel.x + 1, voxel.y, voxel.z + 1)
        )
    def _getBackSide(self, voxel):
        return (
            (voxel.x + 1, voxel.y + 1, voxel.z + 1),
            (voxel.x + 1, voxel.y + 1, voxel.z),
            (voxel.x, voxel.y + 1, voxel.z),
            (voxel.x, voxel.y + 1, voxel.z + 1)
        )
    def _objExposed(self, voxel):
        """ --> a set of [0, 6) representing which voxel faces are shown
            for the meaning of 0-5, see AAQuad.normals
            get the sick truth about these voxels' dirty secrets...
        """
        # check left 0
        side = self.getVoxel(voxel.x - 1, voxel.y, voxel.z)
        s0 = side is None or side.colorIndex == 0
        # check right 1
        side = self.getVoxel(voxel.x + 1, voxel.y, voxel.z)
        s1 = side is None or side.colorIndex == 0
        # check top 2
        side = self.getVoxel(voxel.x, voxel.y, voxel.z + 1)
        s2 = side is None or side.colorIndex == 0
        # check bottom 3
        side = self.getVoxel(voxel.x, voxel.y, voxel.z - 1)
        s3 = side is None or side.colorIndex == 0
        # check front 4
        side = self.getVoxel(voxel.x, voxel.y - 1, voxel.z)
        s4 = side is None or side.colorIndex == 0
        # check back 5
        side = self.getVoxel(voxel.x, voxel.y + 1, voxel.z)
        s5 = side is None or side.colorIndex == 0
        return s0, s1, s2, s3, s4, s5

class Voxel:
    def __init__(self, x, y, z, colorIndex):
        self.x = x
        self.y = y
        self.z = z
        self.colorIndex = colorIndex

def genNormals(self, aaQuads, overwrite=False):
    # compute CCW normal if it doesn't exist
    for face in aaQuads:
        if overwrite or face.normal is None:
            side_a = (face.vertices[1][0] - face.vertices[0][0],
                      face.vertices[1][1] - face.vertices[0][1],
                      face.vertices[1][2] - face.vertices[0][2])
            side_b = (face.vertices[-1][0] - face.vertices[0][0],
                      face.vertices[-1][1] - face.vertices[0][1],
                      face.vertices[-1][2] - face.vertices[0][2])
            # compute the cross product
            face.normal = (side_a[1]*side_b[2] - side_a[2]*side_b[1],
                           side_a[2]*side_b[0] - side_a[0]*side_b[2],
                           side_a[0]*side_b[1] - side_a[1]*side_b[0])

def importObj(stream):
    vertices = []
    faces = []
    uvs = []
    normals = []
    for line in stream:
        # make sure there's no new line or trailing spaces
        l = line.strip().split(' ')
        lineType = l[0].strip()
        data = l[1:]
        if lineType == 'v':
            # vertex
            v = tuple(map(float, data))
            vertices.append(v)
        elif lineType == 'vt':
            # uv
            uvs.append( tuple(map(float, data)) )
        elif lineType == 'vn':
            # normal
            normals.append( tuple(map(float, data)) )
        elif lineType == 'f':
            # face (assume all verts/uvs/normals have been processed)
            faceVerts = []
            faceUvs = []
            faceNormals = []
            for v in data:
                result = v.split('/')
                print(result)
                # recall that everything is 1 indexed...
                faceVerts.append(vertices[int(result[0]) - 1])
                if len(result) == 1:
                    continue # there is only a vertex index
                if result[1] != '':
                    # uvs may not be present, ex: 'f vert//normal ...'
                    faceUvs.append(uvs[int(result[1]) - 1])
                if len(result) <= 2:
                    # don't continue if only vert and uv are present
                    continue
                faceNormals.append(normals[int(result[2]) - 1])
            faces.append( GeoFace(faceVerts, faceUvs, faceNormals) )
        else:
            # there could be material specs, smoothing, or comments... ignore!
            pass
    return faces

def exportObj(stream, aaQuads):
    # gather some of the needed information
    faces = aaQuads
    # copy the normals from AAQuad (99% of cases will use all directions)
    normals = list(AAQuad.normals)
    uvs = set()
    for f in faces:
        if f.uv is not None:
            uvs.add(f.uv)
    # convert this to a list because we need to get their index later
    uvs = list(uvs)
    # we will build a list of vertices as we go and then write everything
    # in bulk, disadvantage that MANY verts will be duplicated in the OBJ file
    fLines = []
    vertices = []
    indexOffset = 0
    for f in faces:
        # recall that OBJ files are 1 indexed
        n = 1 + normals.index(f.normal) if f.normal is not None else ''
        uv = 1 + uvs.index(f.uv) if f.uv is not None else ''
        # this used to be a one liner ;)
        fLine = ['f']
        for i, vert in enumerate(f.vertices):
            # for each vertex of this face
            v = 1 + indexOffset + f.vertices.index(vert)
            fLine.append(str(v) + '/' + str(uv) + '/' + str(n))
        vertices.extend(f.vertices)
        indexOffset += len(f.vertices)
        fLines.append(' '.join(fLine) + '\n')
    # write to the file
    stream.write('# shivshank\'s .obj optimizer\n')
    stream.write('\n')
    if len(normals) > 0:
        stream.write('# normals\n')
        for n in normals:
            stream.write('vn ' + ' '.join(list(map(str, n))) + '\n')
        stream.write('\n')
    if len(uvs) > 0:
        stream.write('# texcoords\n')
        for i in uvs:
            stream.write('vt ' + ' '.join(list(map(str, i))) + '\n')
        stream.write('\n')
    # output the vertices and faces
    stream.write('# verts\n')
    for v in vertices:
        stream.write('v ' + ' '.join(list(map(str, v))) + '\n')
    stream.write('\n')
    stream.write('# faces\n')
    for i in fLines:
        stream.write(i)
    stream.write('\n')
    stream.write('\n')
    return len(vertices), len(fLines)

def importVox(file):
    """ --> a VoxelStruct from this .vox file stream """
    # in theory this could elegantly be many functions and classes
    # but this is such a simple file format...
    # refactor: ? should probably find a better exception type than value error
    vox = VoxelStruct()
    magic = file.read(4)
    if magic != b'VOX ':
        print('magic number is', magic)
        if userAborts('This does not appear to be a VOX file. Abort?'):
            raise ValueError("Invalid magic number")
    # the file appears to use little endian consistent with RIFF
    version = int.from_bytes(file.read(4), byteorder='little')
    if version != 150:
        if userAborts('Only version 150 is supported; this file: '
                      + str(version) + '. Abort?'):
            raise ValueError("Invalid file version")
    mainHeader = _readChunkHeader(file)
    if mainHeader['id'] != b'MAIN':
        print('chunk id:', mainId)
        if userAborts('Did not find the main chunk. Abort?'):
            raise ValueError("Did not find main VOX chunk. ")
    #assert mainHeader['size'] == 0, "main chunk should have size 0"
    # we don't need anything from the size or palette header!
    # : we can figure out (minimum) bounds later from the voxel data
    # : we only need UVs from voxel data; user can export palette elsewhere
    nextHeader = _readChunkHeader(file)
    while nextHeader['id'] != b'XYZI':
        # skip the contents of this header and its children, read the next one
        file.read(nextHeader['size'] + nextHeader['childrenSize'])
        nextHeader = _readChunkHeader(file)
    voxelHeader = nextHeader
    assert voxelHeader['id'] == b'XYZI', 'this should be literally impossible'
    assert voxelHeader['childrenSize'] == 0, 'why voxel chunk have children?'
    seekPos = file.tell()
    totalVoxels = int.from_bytes(file.read(4), byteorder='little')
    ### READ THE VOXELS ###
    for i in range(totalVoxels):
        # n.b., byte order should be irrelevant since these are all 1 byte
        x = int.from_bytes(file.read(1), byteorder='little')
        y = int.from_bytes(file.read(1), byteorder='little')
        z = int.from_bytes(file.read(1), byteorder='little')
        color = int.from_bytes(file.read(1), byteorder='little')
        vox.setVoxel(Voxel(x, y, z, color))
    # assert that we've read the entire voxel chunk
    assert file.tell() - seekPos == voxelHeader['size']
    # (there may be more chunks after this but we don't need them!)
    #print('\tdone reading voxel data;', totalVoxels , 'voxels read ;D')
    return vox
    
def _readChunkHeader(buffer):
    id = buffer.read(4)
    if id == b'':
        raise ValueError("Unexpected EOF, expected chunk header")
    size = int.from_bytes(buffer.read(4), byteorder='little')
    childrenSize = int.from_bytes(buffer.read(4), byteorder='little')
    return {
        'id': id, 'size': size, 'childrenSize': childrenSize
    }

def userAborts(msg):
    print(msg + ' (y/n)')
    u = input()
    if u.startswith('n'):
        return False
    
    return True

def exportAll():
    """ Uses a file to automatically export a bunch of files!
        See this function for details on the what the file looks like.
    """
    import os, os.path

    with open('exporter.txt', mode='r') as file:
        # use this as a file "spec"
        fromSource = os.path.abspath(file.readline().strip())
        toExportDir = os.path.abspath(file.readline().strip())
        optimizing = file.readline()
        if optimizing.lower() == 'true':
            optimizing = True
        else:
            optimizing = False

    print('exporting vox files under', fromSource)
    print('\tto directory', toExportDir)
    print('\toptimizing?', optimizing)
    print()
    
    # export EVERYTHING (.vox) walking the directory structure
    for p, dirList, fileList in os.walk(fromSource):
        pathDiff = os.path.relpath(p, start=fromSource)
        outDir = os.path.join(toExportDir, pathDiff)
        # REFACTOR: the loop should be moved to a function
        for fileName in fileList:
            # only take vox files
            if os.path.splitext(fileName)[1] != '.vox':
                print('ignored', fileName)
                continue
            print('exporting', fileName)
            # read/import the voxel file
            with open(os.path.join(p, fileName), mode='rb') as file:
                try:
                    vox = importVox(file)
                except ValueError as exc:
                    print('aborted', fileName, str(exc))
                    continue
            # mirror the directory structure in the export folder
            if not os.path.exists(outDir):
                os.makedirs(outDir)
                print('\tcreated directory', outDir)
            # export a non-optimized version
            objName = os.path.splitext(fileName)[0]
            rawQuads = vox.toQuads()
            with open(os.path.join(outDir, objName + '.obj'), mode='w') as file:
                vCount, qCount = exportObj(file, rawQuads)
            print('\texported', vCount, 'vertices,', qCount, 'quads')
            if optimizing:
                # TODO
                continue
                optiFaces = optimizedGreedyMesh(rawQuads)
                bucketHash(optiFaces, *vox.getBounds())
                with open(os.path.join(outDir, objName + '.greedy.obj'),
                          mode='w') as file:
                    exportObj(file, optiFaces)

def byPrompt():
    import os, os.path
    from glob import glob

    print('Enter an output path:')
    u = input('> ').strip()
    while not os.path.exists(u):
        print('That path does not exist.')
        print('Enter an output path:')
        u = input('> ').strip()
    outRoot = os.path.abspath(u)
    
    print('Are we optimizing? (y/n)')
    u = input('> ').strip()
    # this could be a one liner but I think it's easier to read this way
    if u.startswith('y'):
        optimizing = True
    else:
        optimizing = False

    try:
        while True:
            print('Enter glob of export files (\'exit\' or blank to quit):')
            u = input('> ').strip()
            if u == 'exit' or u == '':
                break
            u = glob(u)
            for f in u:
                print('reading VOX file', f)
                with open(f, mode='rb') as file:
                    try:
                        vox = importVox(file)
                    except ValueError:
                        print('\tfile reading aborted')
                        continue
                outFile = os.path.splitext(os.path.basename(f))[0]
                outPath = os.path.join(outRoot, outFile)
                print('exporting VOX to OBJ at path', outPath)
                with open(outPath, mode='w') as file:
                    exportObj(file, vox.toQuads())
                if optimizing:
                    # TODO
                    pass
    except KeyboardInterrupt:
        pass

# =========================================================================
# Single-file export mode (added).
#
# Provide one or more full .vox paths on the command line, e.g.:
#     python vox_to_obj_exporter.py "C:/.../Icon_009_01.vox" [more.vox ...]
# and for each input the tool writes 3 sibling files in the SAME folder:
#     <name>.obj  — geometry with mtllib/usemtl referencing <name>.mtl
#     <name>.mtl  — Wavefront material pointing at the palette PNG
#     <name>.png  — 256x1 palette texture (one pixel per palette index)
#
# Falls back to the original exportAll() / byPrompt() flow when called
# with no arguments.
# =========================================================================

def importVoxFull(file):
    """ Like importVox() but also returns the 256-entry RGBA palette
        (or None if the .vox has no RGBA chunk).
    """
    vox = VoxelStruct()
    palette = None

    magic = file.read(4)
    if magic != b'VOX ':
        raise ValueError('not a VOX file (magic=%r)' % magic)
    version = int.from_bytes(file.read(4), byteorder='little')
    if version != 150:
        raise ValueError('unsupported VOX version: %d' % version)

    main_header = _readChunkHeader(file)
    if main_header['id'] != b'MAIN':
        raise ValueError('expected MAIN chunk, got %r' % main_header['id'])
    file.read(main_header['size'])
    end_pos = file.tell() + main_header['childrenSize']

    while file.tell() < end_pos:
        header = _readChunkHeader(file)
        cid = header['id']
        size = header['size']
        body = file.read(size)

        if cid == b'XYZI':
            n = int.from_bytes(body[:4], 'little')
            for i in range(n):
                x = body[4 + i * 4]
                y = body[5 + i * 4]
                z = body[6 + i * 4]
                c = body[7 + i * 4]
                if c != 0:
                    vox.setVoxel(Voxel(x, y, z, c))
        elif cid == b'RGBA':
            palette = []
            for i in range(256):
                r = body[i * 4]
                g = body[i * 4 + 1]
                b = body[i * 4 + 2]
                a = body[i * 4 + 3]
                palette.append((r, g, b, a))

        # skip any nested children
        file.read(header['childrenSize'])

    return vox, palette


def exportObjToStream(stream, aaQuads, mtl_lib=None, material_name=None,
                      scale=1.0, center=False, ground=False, y_up=False):
    """ Same OBJ writer as exportObj(), but optionally emits mtllib / usemtl
        lines so the geometry references an external material file, and
        applies a uniform world transform on every vertex.

        scale  : uniform multiplier on all axes. MagicaVoxel's native OBJ
                 exporter uses 0.1 per voxel, so pass scale=0.1 to match its
                 size. Default 1.0 = raw voxel coordinates (legacy).
        center : if True, centers the model's bounding box on the X and Y
                 axes (the two "horizontal" axes). The vertical axis is
                 not centered.
        ground : if True, shifts the model so the lowest vertical voxel
                 lies at 0 (i.e. the character "stands on the ground plane").
        y_up   : if True, rotates the model -90° around the X axis so that
                 the VOX vertical axis (Z) lands on the OBJ Y axis. Use
                 this for Y-up engines (Unity, Three.js, OBJ's traditional
                 convention, MagicaVoxel's native OBJ output). The rotation
                 is applied AFTER centering/grounding, and the 6 axis-
                 aligned face normals are rotated accordingly.

        Coordinate transform (per vertex):
              vx = (x - off_x) * scale
              vy = (y - off_y) * scale
              vz = (z - off_z) * scale
              if y_up:  return (vx, vz, -vy)        # rotate -90° around X
              else  :   return (vx,  vy,   vz)
    """
    faces = aaQuads
    src_normals = list(AAQuad.normals)
    uvs = set()
    for f in faces:
        if f.uv is not None:
            uvs.add(f.uv)
    uvs = list(uvs)

    # Compute X / Y / Z offsets in VOX space (before any rotation).
    off_x = off_y = off_z = 0.0
    if center or ground:
        xs, ys, zs = [], [], []
        for f in faces:
            for v in f.vertices:
                xs.append(v[0]); ys.append(v[1]); zs.append(v[2])
        if xs:
            if center:
                off_x = (min(xs) + max(xs)) / 2.0
                off_y = (min(ys) + max(ys)) / 2.0
            if ground:
                off_z = float(min(zs))

    def xform_vert(v):
        vx = (v[0] - off_x) * scale
        vy = (v[1] - off_y) * scale
        vz = (v[2] - off_z) * scale
        if y_up:
            # rotate -90° around X: (x, y, z) -> (x, z, -y)
            return (vx, vz, -vy)
        return (vx, vy, vz)

    def xform_normal(n):
        if y_up:
            return (n[0], n[2], -n[1])
        return n

    fLines = []
    vertices = []
    indexOffset = 0
    for f in faces:
        n = 1 + src_normals.index(f.normal) if f.normal is not None else ''
        uv = 1 + uvs.index(f.uv) if f.uv is not None else ''
        fLine = ['f']
        for i, vert in enumerate(f.vertices):
            v = 1 + indexOffset + f.vertices.index(vert)
            fLine.append(str(v) + '/' + str(uv) + '/' + str(n))
        vertices.extend(f.vertices)
        indexOffset += len(f.vertices)
        fLines.append(' '.join(fLine) + '\n')

    stream.write('# generated by vox_to_obj_exporter\n')
    stream.write('# scale=%g center=%s ground=%s y_up=%s\n' %
                 (scale,
                  str(bool(center)).lower(),
                  str(bool(ground)).lower(),
                  str(bool(y_up)).lower()))
    stream.write('\n')
    if mtl_lib:
        stream.write('mtllib %s\n' % mtl_lib)
    if material_name:
        stream.write('usemtl %s\n' % material_name)
    if mtl_lib or material_name:
        stream.write('\n')

    if len(src_normals) > 0:
        stream.write('# normals\n')
        for n in src_normals:
            tn = xform_normal(n)
            stream.write('vn %s %s %s\n' % (_fmt(tn[0]), _fmt(tn[1]), _fmt(tn[2])))
        stream.write('\n')
    if len(uvs) > 0:
        stream.write('# texcoords\n')
        for i in uvs:
            stream.write('vt ' + ' '.join(list(map(str, i))) + '\n')
        stream.write('\n')
    stream.write('# verts\n')
    for v in vertices:
        tv = xform_vert(v)
        stream.write('v %s %s %s\n' % (_fmt(tv[0]), _fmt(tv[1]), _fmt(tv[2])))
    stream.write('\n')
    stream.write('# faces\n')
    for i in fLines:
        stream.write(i)
    stream.write('\n')
    return len(vertices), len(fLines)


def _fmt(x):
    """Format a float coordinate compactly (drop trailing zeros, keep up to 6 dp)."""
    s = ('%.6f' % x).rstrip('0').rstrip('.')
    return s if s else '0'


def writeMtl(mtl_path, palette_png_basename, material_name='palette'):
    """ Write a minimal Wavefront .mtl that maps the diffuse channel to
        the 256x1 palette PNG produced by writePalettePng().
    """
    with open(mtl_path, 'w') as f:
        f.write('# generated by vox_to_obj_exporter\n')
        f.write('\n')
        f.write('newmtl %s\n' % material_name)
        f.write('illum 1\n')
        f.write('Ka 0.000 0.000 0.000\n')
        f.write('Kd 1.000 1.000 1.000\n')
        f.write('Ks 0.000 0.000 0.000\n')
        f.write('map_Kd %s\n' % palette_png_basename)


def writePalettePng(png_path, palette):
    """ Write a 256x1 PNG where pixel x=i is palette index i+1's colour
        (matches MagicaVoxel's exporter convention).
    """
    try:
        from PIL import Image
    except ImportError:
        raise SystemExit(
            'Need Pillow for palette PNG output. Install with: pip install Pillow'
        )
    if palette is None:
        # No RGBA chunk in VOX -> fall back to opaque white so the model
        # at least renders.
        palette = [(255, 255, 255, 255)] * 256

    im = Image.new('RGBA', (256, 1))
    for i in range(256):
        if i < len(palette):
            r, g, b, a = palette[i]
        else:
            r, g, b, a = 0, 0, 0, 255
        # opaque always — transparent palette pixels confuse OBJ viewers
        im.putpixel((i, 0), (r, g, b, 255))
    im.save(png_path)


def exportVoxFile(vox_path, material_name='palette',
                  scale=0.1, center=True, ground=True, y_up=False):
    """ Top-level convenience API.

        Given the FULL path to a .vox file, write three sibling files in
        the same directory:
            <stem>.obj   (geometry + mtllib reference)
            <stem>.mtl   (material pointing at <stem>.png)
            <stem>.png   (256x1 palette texture)

        Defaults match MagicaVoxel's native OBJ export size/centring:
            scale  = 0.1   (1 voxel -> 0.1 world units)
            center = True  (model centred on origin in X and Y)
            ground = True  (lowest vertical voxel sits at Z = 0)
            y_up   = False (keep VOX's Z-up convention)
        Pass y_up=True for a Y-up orientation (Unity / Three.js).
        Pass scale=1.0, center=False, ground=False to get raw voxel coords.

        Returns (obj_path, mtl_path, png_path, n_vertices, n_quads).
    """
    import os
    base, ext = os.path.splitext(vox_path)
    if ext.lower() != '.vox':
        raise ValueError('not a .vox file: %s' % vox_path)

    obj_path = base + '.obj'
    mtl_path = base + '.mtl'
    png_path = base + '.png'
    mtl_name = os.path.basename(mtl_path)
    png_name = os.path.basename(png_path)

    with open(vox_path, 'rb') as f:
        vox, palette = importVoxFull(f)

    quads = vox.toQuads()
    with open(obj_path, 'w') as f:
        n_v, n_q = exportObjToStream(f, quads,
                                     mtl_lib=mtl_name,
                                     material_name=material_name,
                                     scale=scale, center=center,
                                     ground=ground, y_up=y_up)
    writeMtl(mtl_path, png_name, material_name)
    writePalettePng(png_path, palette)
    return obj_path, mtl_path, png_path, n_v, n_q


if __name__ == "__main__":
    import sys, os

    # Single-file mode: any .vox path on the command line triggers it.
    # All non-.vox arguments are treated as flags (parsed below).
    vox_args = [a for a in sys.argv[1:] if a.lower().endswith('.vox')]
    if vox_args:
        import argparse
        parser = argparse.ArgumentParser(
            prog='vox_to_obj_exporter.py',
            description=('Convert one or more .vox files into .obj + .mtl + '
                         '256x1 palette .png, written next to each .vox.'))
        parser.add_argument('vox', nargs='+',
            help='full paths to .vox files')
        parser.add_argument('--scale', type=float, default=0.1, metavar='S',
            help='world units per voxel (default 0.1, matches MagicaVoxel)')
        parser.add_argument('--no-center', action='store_true',
            help='do NOT centre the model on X/Y; keep raw voxel positions')
        parser.add_argument('--no-ground', action='store_true',
            help='do NOT shift Z so the bottom voxel sits at Z=0')
        parser.add_argument('--y-up', action='store_true', dest='y_up',
            help='rotate the model -90° around X so that VOX Z (up) maps '
                 'to OBJ Y. Use this for Unity / Three.js / any Y-up engine '
                 '(matches MagicaVoxel native OBJ output orientation).')
        args = parser.parse_args()

        center = not args.no_center
        ground = not args.no_ground
        print('scale=%g  center=%s  ground=%s  y_up=%s' %
              (args.scale, str(center).lower(), str(ground).lower(),
               str(args.y_up).lower()))
        for vox_path in args.vox:
            if not os.path.isfile(vox_path):
                print('not found:', vox_path)
                continue
            try:
                obj, mtl, png, nv, nq = exportVoxFile(
                    vox_path, scale=args.scale, center=center,
                    ground=ground, y_up=args.y_up)
            except ValueError as exc:
                print('skipped', vox_path, '-', exc)
                continue
            print('%s  (%d verts, %d quads)' % (os.path.basename(obj), nv, nq))
            print('  +', os.path.basename(mtl))
            print('  +', os.path.basename(png))
        sys.exit(0)

    # Legacy modes preserved.
    profiling = False
    try:
        import cProfile
        if profiling:
            cProfile.run('exportAll()', sort='tottime')
        else:
            exportAll()
    except OSError:
        print('No instruction file found, falling back to prompt.')
        byPrompt()