<template>
  <div class="file-preview-component" v-loading="loading">
    <div class="toolbar">
      <div class="left-actions">
        <el-button-group v-if="showZoom">
          <el-button :icon="ZoomOut" @click="zoomOut" title="缩小" />
          <el-button class="zoom-text">{{ Math.round(scale * 100) }}%</el-button>
          <el-button :icon="ZoomIn" @click="zoomIn" title="放大" />
        </el-button-group>
      </div>
      
      <div class="right-actions">
        <el-button :icon="Download" @click="handleDownload">下载</el-button>
        <el-button :icon="Printer" @click="handlePrint">打印</el-button>
      </div>
    </div>
    
    <div 
      class="preview-content-wrapper" 
      ref="wrapperRef" 
      :class="{ 'full-height': fileType === 'pdf', 'is-image': fileType === 'image', 'is-markdown': fileType === 'markdown', 'is-docx': fileType === 'docx' }"
    >
      <!-- Docx Preview -->
      <div 
        v-if="fileType === 'docx'"
        ref="docxContainer" 
        class="docx-container"
        :style="docxStyle"
      ></div>

      <!-- PDF Preview -->
      <iframe 
        v-if="fileType === 'pdf'"
        :src="pdfUrl" 
        class="pdf-frame"
      ></iframe>

      <!-- Excel Preview -->
      <div 
        v-if="fileType === 'excel'" 
        class="excel-wrapper"
      >
        <el-tabs 
          v-if="excelWorkbook && excelWorkbook.SheetNames.length > 1" 
          v-model="activeSheet" 
          class="excel-tabs"
        >
          <el-tab-pane 
            v-for="sheet in excelWorkbook.SheetNames" 
            :key="sheet" 
            :label="sheet" 
            :name="sheet" 
          />
        </el-tabs>
        <div class="excel-container" ref="excelContainerRef">
          <div :style="excelStyle" v-html="excelHtml" class="excel-content"></div>
        </div>
      </div>

      <!-- Image Preview -->
      <div 
        v-if="fileType === 'image'" 
        class="image-container"
      >
        <img 
          :src="imageUrl" 
          :style="scaleStyle" 
          alt="preview" 
        />
      </div>

      <!-- Markdown Preview -->
      <div v-if="fileType === 'markdown'" class="markdown-container">
        <div class="markdown-content" v-html="markdownHtml"></div>
      </div>

      <!-- PPT Preview -->
      <div 
        v-if="['ppt', 'pptx'].includes(fileType)"
        class="ppt-container"
      >
        <VueOfficePptx 
          :src="pptSrc"
          @rendered="onPptRendered"
          @error="onPptError"
          style="width: 100%; height: 100%;"
        />
      </div>

      <!-- Fallback / PPT -->
      <div 
        v-if="shouldShowFallback" 
        class="fallback-container"
      >
        <el-empty :description="fallbackDescription">
           <el-button type="primary" :icon="Download" @click="handleDownload">下载文件</el-button>
        </el-empty>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed, nextTick } from 'vue'
import { renderAsync } from 'docx-preview'
import * as XLSX from 'xlsx'
import VueOfficePptx from '@vue-office/pptx'
// import '@vue-office/pptx/lib/index.css'
import { ZoomIn, ZoomOut, Download, Printer } from '@element-plus/icons-vue'
import request from '../../utils/request'
import { ElMessage } from 'element-plus'
import MarkdownIt from 'markdown-it'

const props = defineProps<{
  fileId: number
  fileName: string
  fileType: 'doc' | 'docx' | 'pdf' | 'excel' | 'image' | 'ppt' | 'markdown' | 'unknown'
}>()

const loading = ref(false)
const scale = ref(1.0)

// Docx refs
const docxContainer = ref<HTMLElement | null>(null)
const wrapperRef = ref<HTMLElement | null>(null)

// Excel refs
const excelHtml = ref('')
const excelWorkbook = ref<any>(null)
const activeSheet = ref('')
const renderError = ref(false)
const excelContainerRef = ref<HTMLElement | null>(null)
const markdownHtml = ref('')

const markdown = new MarkdownIt({
  html: false,
  linkify: true,
  typographer: true
})

// Computed
const showZoom = computed(() => ['docx', 'excel', 'image'].includes(props.fileType))

const shouldShowFallback = computed(() => {
  if (loading.value) return false
  if (renderError.value) return true
  if (['doc', 'unknown'].includes(props.fileType)) return true
  return false
})

const fallbackDescription = computed(() => {
  if (props.fileType === 'doc') return 'DOC 暂不支持在线预览，请下载后查看'
  return '该文件类型暂不支持在线预览'
})

const pptSrc = computed(() => {
  if (!['ppt', 'pptx'].includes(props.fileType)) return ''
  const token = localStorage.getItem('token')
  return `${request.defaults.baseURL || '/api'}/file/download/${props.fileId}?access_token=${token}`
})

const docxStyle = computed(() => ({
  transform: `scale(${scale.value})`,
  transformOrigin: 'top center',
  width: '100%',
  maxWidth: '1000px', // More like web document
  margin: '0 auto'
}))

const excelStyle = computed(() => ({
  // Use zoom for Excel as it handles scroll area much better than transform:scale for tables
  // Zoom is non-standard but widely supported in Chromium-based browsers (which most use)
  zoom: scale.value,
  display: 'inline-block',
  minWidth: '100%'
}))

const scaleStyle = computed(() => ({
  transform: `scale(${scale.value})`,
  transformOrigin: 'top center',
  display: 'inline-block'
}))

const pdfUrl = computed(() => {
  if (props.fileType !== 'pdf') return ''
  const token = localStorage.getItem('token')
  return `${request.defaults.baseURL || '/api'}/file/download/${props.fileId}?access_token=${token}&preview=true`
})

const imageUrl = ref('')

// Methods
const zoomIn = () => {
  if (scale.value < 3.0) scale.value += 0.1
}

const zoomOut = () => {
  if (scale.value > 0.2) scale.value -= 0.1
}

const onPptRendered = () => {
  loading.value = false
}

const onPptError = (e: any) => {
  console.error('PPT render error:', e)
  renderError.value = true
  loading.value = false
}

const renderExcelSheet = (sheetName: string) => {
  if (!excelWorkbook.value || !sheetName) return
  const worksheet = excelWorkbook.value.Sheets[sheetName]
  
  // Check if worksheet exists and has data
  // Empty worksheets exist but have no !ref (range) property
  if (!worksheet || !worksheet['!ref']) {
    excelHtml.value = '<div class="empty-sheet">此工作表没有内容</div>'
    return
  }
  
  // Convert to HTML
  excelHtml.value = XLSX.utils.sheet_to_html(worksheet)
}

watch(activeSheet, async (newSheet) => {
  if (newSheet) {
    renderExcelSheet(newSheet)
    await nextTick()
    if (excelContainerRef.value) {
      excelContainerRef.value.scrollTo(0, 0)
    }
  }
})

const handleDownload = () => {
  const link = document.createElement('a')
  link.href = `${request.defaults.baseURL || '/api'}/file/download/${props.fileId}`
  const token = localStorage.getItem('token')
  if (token) link.href += `?access_token=${token}`
  link.setAttribute('download', props.fileName)
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

const handlePrint = () => {
  if (props.fileType === 'pdf') {
    // For PDF, the most reliable way across browsers is to open in a new tab 
    // where the browser's native PDF viewer can handle printing perfectly.
    const token = localStorage.getItem('token')
    const url = `${request.defaults.baseURL || '/api'}/file/download/${props.fileId}?access_token=${token}&preview=true`
    window.open(url, '_blank')
    return
  }
  
  if (props.fileType === 'image') {
    const printWindow = window.open('', '_blank')
    if (printWindow) {
      printWindow.document.write(`
        <html>
          <head>
            <title>${props.fileName}</title>
            <style>
              @media print { body { margin: 0; } }
              html, body {
                margin: 0;
                padding: 0;
                background: #ffffff;
                display: flex;
                align-items: center;
                justify-content: center;
              }
              img { max-width: 100%; max-height: 100%; }
            </style>
          </head>
          <body>
            <img src="${imageUrl.value}" />
          </body>
        </html>
      `)
      printWindow.document.close()
      printWindow.focus()
      setTimeout(() => {
        printWindow.print()
        printWindow.close()
      }, 500)
    }
    return
  }

  // Docx and Excel (HTML print)
  let content = ''
  if (props.fileType === 'docx' && docxContainer.value) {
    content = docxContainer.value.innerHTML
  } else if (props.fileType === 'excel') {
    content = excelHtml.value
  } else if (props.fileType === 'markdown') {
    content = markdownHtml.value
  }

  if (content) {
    const printWindow = window.open('', '_blank')
    if (printWindow) {
      printWindow.document.write(`
        <html>
          <head>
            <title>${props.fileName}</title>
            <style>
              @media print { body { margin: 0; } }
              body { margin: 0; padding: 0; font-family: sans-serif; background: #ffffff; }
              table { border-collapse: collapse; width: 100%; }
              td, th { border: 1px solid #ddd; padding: 8px; }
              /* Add more styles as needed */
            </style>
          </head>
          <body>${content}</body>
        </html>
      `)
      printWindow.document.close()
      printWindow.focus()
      setTimeout(() => {
        printWindow.print()
        printWindow.close()
      }, 500)
    }
  } else {
    // PPT or unknown
    ElMessage.info('该文件类型不支持在线打印，请下载后打印')
    handleDownload()
  }
}

const loadContent = async () => {
  loading.value = true
  renderError.value = false
  if (props.fileType !== 'markdown') {
    markdownHtml.value = ''
  }
  try {
    if (props.fileType === 'docx') {
      const res = await request.get(`/file/download/${props.fileId}`, { responseType: 'blob' })
      await nextTick()
      if (docxContainer.value) {
        docxContainer.value.innerHTML = ''
        docxContainer.value.style.opacity = '0'
        let rendered = false
        try {
          await renderAsync(res as unknown as Blob, docxContainer.value, docxContainer.value, {
            className: 'docx-content',
            inWrapper: true,
            ignoreWidth: false,
            ignoreHeight: true,
            experimental: false,
            useBase64URL: false
          })
          rendered = true
        } catch (firstError) {
          console.warn('docx first pass failed, retrying with compatibility mode', firstError)
          docxContainer.value.innerHTML = ''
          await renderAsync(res as unknown as Blob, docxContainer.value, docxContainer.value, {
            className: 'docx-content',
            inWrapper: true,
            ignoreWidth: false,
            ignoreHeight: true,
            experimental: true,
            useBase64URL: true
          })
          rendered = true
        }
        if (rendered) {
          await nextTick()
          if (wrapperRef.value) {
            wrapperRef.value.scrollTop = 0
            wrapperRef.value.scrollLeft = 0
          }
          docxContainer.value.scrollTop = 0
          docxContainer.value.scrollLeft = 0
        }
        docxContainer.value.style.opacity = '1'
      }
    } else if (props.fileType === 'excel') {
      const res = await request.get(`/file/download/${props.fileId}`, { responseType: 'arraybuffer' })
      const data = new Uint8Array(res as unknown as ArrayBuffer)
      const workbook = XLSX.read(data, { type: 'array' })
      excelWorkbook.value = workbook
      if (workbook.SheetNames.length > 0) {
        const firstSheet = workbook.SheetNames[0]
        if (firstSheet) {
          const isSameSheet = activeSheet.value === firstSheet
          activeSheet.value = firstSheet
          // If name is same, watcher won't fire, so we force render
          if (isSameSheet) {
            renderExcelSheet(firstSheet)
          }
        }
      }
    } else if (props.fileType === 'image') {
      // Explicitly fetch image to handle auth headers and catch errors
      const res = await request.get(`/file/download/${props.fileId}`, { responseType: 'blob' })
      if (imageUrl.value) URL.revokeObjectURL(imageUrl.value)
      imageUrl.value = URL.createObjectURL(res as unknown as Blob)
    } else if (props.fileType === 'markdown') {
      const res = await request.get(`/file/download/${props.fileId}`, {
        params: { preview: true },
        responseType: 'text'
      })
      markdownHtml.value = markdown.render((res as unknown as string) || '')
    }
    // PDF handled by src binding (iframe usually handles cookies/auth differently, or via query param)
  } catch (error) {
    console.error(error)
    ElMessage.error('加载失败')
    renderError.value = true
  } finally {
    loading.value = false
  }
}

watch(() => props.fileId, loadContent)
onMounted(loadContent)
</script>

<style scoped lang="scss">
.file-preview-component {
  flex: 1;
  display: flex;
  flex-direction: column;
  background-color: var(--pan-bg);
  position: relative;
  min-height: 0;
}

.toolbar {
  height: 56px;
  padding: 0 24px;
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--pan-border);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
  z-index: 100;
  
  :deep(.el-button) {
    background-color: transparent !important;
    border-color: var(--pan-border-strong) !important;
    color: var(--pan-text-body) !important;
    
    &:hover {
      color: var(--pan-primary) !important;
      border-color: var(--pan-primary) !important;
      background-color: rgba(16, 185, 129, 0.05) !important;
    }
  }

  .zoom-text {
    width: 60px;
    text-align: center;
    font-weight: 600;
    font-family: var(--font-mono);
    color: var(--pan-text-main);
    font-size: 13px;
    cursor: default;
    border-left: none !important;
    border-right: none !important;
  }
}

.preview-content-wrapper {
  flex: 1;
  padding: 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center; /* Center the content */
  overflow: hidden;
  background-color: #0c0d0d; 
  min-height: 0;

  &.full-height {
    padding: 0;
    align-items: stretch;
  }

  &.is-image {
    padding: 24px 0;
    background: transparent;
    justify-content: flex-start;
  }

  &.is-markdown {
    background: transparent;
    align-items: stretch;
    justify-content: flex-start;
  }

  &.is-docx {
    align-items: center;
    justify-content: flex-start;
    overflow: auto;
    background: transparent;
  }
}

.image-container {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  overflow: hidden;

  img {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
    border-radius: 4px;
    transition: transform 0.2s;
  }
}

.markdown-container {
  width: 100%;
  height: 100%;
  overflow: auto;
  padding: 0;
  background: transparent !important;
  display: flex;
  justify-content: center;
}

.markdown-content {
  width: min(960px, 100%);
  background: transparent;
  border: none;
  border-radius: 0;
  padding: 0;
  color: var(--pan-text-body);
  line-height: 1.7;
  font-size: 14px;

  :deep(h1, h2, h3, h4, h5, h6) {
    color: var(--pan-text-main);
    margin: 14px 0 8px;
    font-weight: 700;
    line-height: 1.35;
  }

  :deep(h1:first-child, h2:first-child, h3:first-child, h4:first-child, h5:first-child, h6:first-child) {
    margin-top: 0;
  }

  :deep(hr) {
    display: none;
  }

  :deep(p) {
    margin: 10px 0;
  }

  :deep(a) {
    color: var(--pan-primary);
    text-decoration: none;
  }

  :deep(code) {
    background: rgba(255, 255, 255, 0.06);
    padding: 2px 6px;
    border-radius: 4px;
    font-family: var(--font-mono);
  }

  :deep(pre) {
    background: rgba(255, 255, 255, 0.03);
    border: none;
    border-radius: 6px;
    padding: 12px;
    overflow: auto;
  }

  :deep(pre code) {
    background: transparent;
    padding: 0;
  }

  :deep(blockquote) {
    border-left: none;
    margin: 12px 0;
    padding-left: 0;
    color: var(--pan-text-muted);
  }

  :deep(ul, ol) {
    padding-left: 20px;
    margin: 10px 0;
  }

  :deep(table) {
    width: 100%;
    border-collapse: collapse;
    margin: 12px 0;
  }

  :deep(th, td) {
    border: 1px solid var(--pan-border);
    padding: 8px 10px;
  }

  :deep(th) {
    background: rgba(255, 255, 255, 0.04);
    font-weight: 600;
    color: var(--pan-text-main);
  }
}

.docx-container {
  background: white;
  border-radius: var(--pan-radius-md);
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
  transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  
  :deep(.docx-wrapper) { background: transparent !important; padding: 0 !important; }
  :deep(.docx) {
    background: white !important;
    margin: 0 auto !important;
    border-radius: var(--pan-radius-md);
  }
}

.pdf-frame {
  width: 100%;
  height: 100%;
  border: none;
  background: white;
}

.excel-wrapper {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  background: white;
  border-radius: var(--pan-radius-md);
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
}

.excel-tabs {
  background-color: #f8f9fa;
  border-bottom: 1px solid #dee2e6;
  padding: 0 16px;
  :deep(.el-tabs__header) { margin: 0; }
  :deep(.el-tabs__item) {
    color: #6b7280;
    font-weight: 600;
  }
  :deep(.el-tabs__item.is-active) {
    color: #10b981;
    font-weight: 700;
  }
  :deep(.el-tabs__active-bar) {
    background-color: #10b981;
    height: 2px;
  }
}

.excel-container {
  flex: 1;
  overflow: auto;
  padding: 16px;
  
  :deep(table) {
    border-collapse: collapse;
    font-size: 13px;
    color: #333;
    background: white;
    
    td, th {
      border: 1px solid #dee2e6;
      padding: 8px 12px;
      min-width: 80px;
    }

    th {
      background-color: #f8f9fa;
      font-weight: 700;
      position: sticky;
      top: 0;
      z-index: 1;
    }

    tr:hover td { background-color: rgba(16, 185, 129, 0.05); }
  }
}

.ppt-container {
  width: 100%;
  height: 100%;
  background: white;
  border-radius: var(--pan-radius-md);
  overflow: hidden;
}

.fallback-container {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
</style>
