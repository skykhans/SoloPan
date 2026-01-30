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
      :class="{ 'full-height': fileType === 'pdf' }"
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
          @tab-change="renderExcelSheet"
          class="excel-tabs"
        >
          <el-tab-pane 
            v-for="sheet in excelWorkbook.SheetNames" 
            :key="sheet" 
            :label="sheet" 
            :name="sheet" 
          />
        </el-tabs>
        <div class="excel-container" :style="scaleStyle" v-html="excelHtml"></div>
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
        <el-empty description="该文件类型暂不支持在线预览">
           <el-button type="primary" @click="handleDownload">下载文件</el-button>
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

const props = defineProps<{
  fileId: number
  fileName: string
  fileType: 'docx' | 'pdf' | 'excel' | 'image' | 'ppt' | 'unknown'
}>()

const loading = ref(false)
const scale = ref(1.0)
const wrapperRef = ref<HTMLElement | null>(null)

// Docx refs
const docxContainer = ref<HTMLElement | null>(null)

// Excel refs
const excelHtml = ref('')
const excelWorkbook = ref<any>(null)
const activeSheet = ref('')
const renderError = ref(false)

// Computed
const showZoom = computed(() => ['docx', 'excel', 'image'].includes(props.fileType))

const shouldShowFallback = computed(() => {
  if (loading.value) return false
  if (renderError.value) return true
  if (['unknown'].includes(props.fileType)) return true
  return false
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

const scaleStyle = computed(() => ({
  transform: `scale(${scale.value})`,
  transformOrigin: 'top center'
}))

const pdfUrl = computed(() => {
  if (props.fileType !== 'pdf') return ''
  const token = localStorage.getItem('token')
  return `${request.defaults.baseURL || '/api'}/file/download/${props.fileId}?access_token=${token}&preview=true`
})

const imageUrl = computed(() => {
  if (props.fileType !== 'image') return ''
  const token = localStorage.getItem('token')
  return `${request.defaults.baseURL || '/api'}/file/download/${props.fileId}?access_token=${token}`
})

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

const renderExcelSheet = (name: any) => {
  // tab-change event returns name directly
  const sheetName = typeof name === 'string' ? name : name.props.name
  if (!excelWorkbook.value) return
  const worksheet = excelWorkbook.value.Sheets[sheetName]
  excelHtml.value = XLSX.utils.sheet_to_html(worksheet)
}

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
    // PDF iframe print
    const iframe = document.querySelector('.pdf-frame') as HTMLIFrameElement
    if (iframe && iframe.contentWindow) {
      iframe.contentWindow.print()
    }
    return
  }
  
  if (props.fileType === 'image') {
    const printWindow = window.open('', '_blank')
    if (printWindow) {
      printWindow.document.write(`<img src="${imageUrl.value}" style="max-width:100%"/>`)
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
  }

  if (content) {
    const printWindow = window.open('', '_blank')
    if (printWindow) {
      printWindow.document.write(`
        <html>
          <head>
            <title>${props.fileName}</title>
            <style>
              body { margin: 20px; font-family: sans-serif; }
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
  try {
    if (props.fileType === 'docx') {
      const res = await request.get(`/file/download/${props.fileId}`, { responseType: 'blob' })
      await nextTick()
      if (docxContainer.value) {
        docxContainer.value.innerHTML = ''
        await renderAsync(res as unknown as Blob, docxContainer.value, docxContainer.value, {
          className: 'docx-content',
          inWrapper: true,
          ignoreWidth: false,
          ignoreHeight: false,
          experimental: true,
          useBase64URL: true,
          padding: 20
        })
      }
    } else if (props.fileType === 'excel') {
      const res = await request.get(`/file/download/${props.fileId}`, { responseType: 'arraybuffer' })
      const data = new Uint8Array(res as unknown as ArrayBuffer)
      const workbook = XLSX.read(data, { type: 'array' })
      excelWorkbook.value = workbook
      if (workbook.SheetNames.length > 0) {
        activeSheet.value = workbook.SheetNames[0]
        renderExcelSheet(workbook.SheetNames[0])
      }
    }
    // PDF and Image handled by src binding
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
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: white; /* White background as requested */
}

.toolbar {
  padding: 10px 20px;
  background-color: white;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  z-index: 100;
  
  .zoom-text {
    width: 60px;
    cursor: default;
  }
}

.preview-content-wrapper {
  flex: 1;
  overflow: auto;
  padding: 20px;
  display: flex;
  justify-content: center;
  align-items: flex-start;

  &.full-height {
    padding: 0;
    align-items: stretch;
    overflow: hidden;
  }
}

.docx-container {
  /* Remove shadow and fixed width limit to make it more like web doc */
  box-shadow: none; 
  background: white;
  
  :deep(.docx-wrapper) {
    background: transparent !important;
    padding: 0 !important;
  }
}

.pdf-frame {
  width: 100%;
  height: 100%;
  border: none;
}

.ppt-container {
  width: 100%;
  height: 100%;
  overflow: hidden;
}

.excel-wrapper {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.excel-tabs {
  padding: 0 20px;
  background-color: #f5f7fa;
  border-bottom: 1px solid #e4e7ed;
  flex-shrink: 0;
}

.excel-container {
  width: 100%;
  overflow: auto;
  flex: 1;
  padding: 20px;
  background: white;
  
  :deep(table) {
    border-collapse: collapse;
    width: 100%;
    
    td, th {
      border: 1px solid #e0e0e0;
      padding: 8px;
    }
  }
}

.image-container {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  
  img {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
  }
}

.fallback-container {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
}
</style>